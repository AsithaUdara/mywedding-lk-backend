using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.UploadBookingContract;

public class UploadBookingContractCommandHandler
    : IRequestHandler<UploadBookingContractCommand, UploadBookingContractResult>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IBookingContractRepository _contractRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IQuotePdfGenerator _quotePdfGenerator;
    private readonly ICloudinaryMediaStorage _cloudinaryMediaStorage;
    private readonly IUnitOfWork _unitOfWork;

    public UploadBookingContractCommandHandler(
        IVendorBookingRepository bookingRepository,
        IBookingContractRepository contractRepository,
        IWeddingEventRepository eventRepository,
        IQuotePdfGenerator quotePdfGenerator,
        ICloudinaryMediaStorage cloudinaryMediaStorage,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _contractRepository = contractRepository;
        _eventRepository = eventRepository;
        _quotePdfGenerator = quotePdfGenerator;
        _cloudinaryMediaStorage = cloudinaryMediaStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<UploadBookingContractResult> Handle(
        UploadBookingContractCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException("VendorBooking", request.BookingId);

        if (booking.VendorService?.Vendor?.UserId != request.VendorUserId)
            throw new ForbiddenAccessException();

        if (booking.Status is not (BookingStatus.AwaitingPayment or BookingStatus.ContractSigned))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Contract can only be uploaded after the booking is accepted and before payment is confirmed."]
            });
        }

        byte[] pdfBytes;
        if (request.GenerateStandardContract)
        {
            var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(booking.EventId, cancellationToken);
            var vendorName = booking.VendorService?.Vendor?.BusinessName ?? "Vendor";
            var serviceName = booking.VendorService?.ServiceName ?? "Service";
            var reference = $"C-{DateTime.UtcNow:yyyyMMdd}-{booking.Id.ToString("N")[..6].ToUpperInvariant()}";
            var body = $"""
                Service Agreement — {reference}
                Vendor: {vendorName}
                Service: {serviceName}
                Event: {weddingEvent?.EventName ?? "Wedding event"}
                Service date: {booking.ServiceDate:yyyy-MM-dd}
                Agreed amount: LKR {booking.FinalAmount:N0}

                By signing electronically, the client agrees to the terms outlined in this document
                and authorizes MyWedding.lk to record the signature for audit purposes.
                """;

            pdfBytes = _quotePdfGenerator.Generate(new InquiryQuotePdfModel(
                reference,
                vendorName,
                "MyWedding.lk",
                "Booking contract",
                booking.FinalAmount,
                "LKR",
                booking.BookedBy?.Email ?? "client@mywedding.lk",
                weddingEvent?.EventName,
                body));
        }
        else if (request.PdfBytes is { Length: > 0 })
        {
            pdfBytes = request.PdfBytes;
        }
        else
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["A PDF file is required, or enable GenerateStandardContract."]
            });
        }

        if (pdfBytes.Length > 10 * 1024 * 1024)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["Contract PDF must be 10 MB or smaller."]
            });
        }

        var publicId = $"booking-{booking.Id:N}";
        var pdfUrl = await _cloudinaryMediaStorage.UploadPdfAsync(
            pdfBytes,
            $"mywedding/contracts/{request.VendorUserId}",
            publicId,
            cancellationToken);

        var now = DateTime.UtcNow;
        var contract = await _contractRepository.GetByBookingIdAsync(booking.Id, cancellationToken);
        if (contract is null)
        {
            contract = new BookingContract
            {
                Id = booking.Id,
                ContractFileUrl = pdfUrl,
                PdfContent = pdfBytes,
                CreatedAt = now
            };
            await _contractRepository.AddAsync(contract, cancellationToken);
        }
        else
        {
            contract.ContractFileUrl = pdfUrl;
            contract.PdfContent = pdfBytes;
            contract.VendorSignedAt = null;
            contract.ClientSignedAt = null;
            contract.CreatedAt = now;
            _contractRepository.Update(contract);
        }

        if (booking.Status == BookingStatus.ContractSigned)
        {
            booking.Status = BookingStatus.AwaitingPayment;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadBookingContractResult
        {
            BookingId = booking.Id,
            EventId = booking.EventId,
            ContractFileUrl = pdfUrl,
            UploadedAtUtc = now,
            SentToClient = false
        };
    }
}
