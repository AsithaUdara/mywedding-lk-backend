using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Contracts.Queries.GetBookingContractFile;

public class GetBookingContractFileQueryHandler : IRequestHandler<GetBookingContractFileQuery, BookingContractFileDto?>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IContractFileFetcher _contractFileFetcher;

    public GetBookingContractFileQueryHandler(
        IVendorBookingRepository bookingRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingEventRepository eventRepository,
        IContractFileFetcher contractFileFetcher)
    {
        _bookingRepository = bookingRepository;
        _organizerRepository = organizerRepository;
        _eventRepository = eventRepository;
        _contractFileFetcher = contractFileFetcher;
    }

    public async Task<BookingContractFileDto?> Handle(
        GetBookingContractFileQuery request,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
            return null;

        var isVendor = booking.VendorService?.Vendor?.UserId == request.UserId;
        var isBooker = booking.BookedById == request.UserId;
        var organizer = await _organizerRepository.GetOrganizerAsync(
            booking.EventId,
            request.UserId,
            cancellationToken);
        var isPlanner = await _eventRepository.IsManagedByPlannerAsync(
            booking.EventId,
            request.UserId,
            cancellationToken);

        if (!isVendor && !isBooker && organizer is null && !isPlanner)
            throw new ForbiddenAccessException();

        var contract = booking.BookingContract;
        if (contract is null || (string.IsNullOrWhiteSpace(contract.ContractFileUrl) && contract.PdfContent is null))
            return null;

        if (!isVendor && contract.VendorSignedAt is null)
            throw new ForbiddenAccessException("The vendor has not sent this contract yet.");

        var pdfBytes = contract.PdfContent;
        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            if (string.IsNullOrWhiteSpace(contract.ContractFileUrl))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["contract"] = ["Contract PDF is not available. Ask the vendor to re-upload it."]
                });
            }

            try
            {
                pdfBytes = await _contractFileFetcher.DownloadAsync(contract.ContractFileUrl, cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["contract"] = ["Could not load the contract PDF from storage. Ask the vendor to re-upload it."]
                });
            }
        }

        if (!IsValidPdf(pdfBytes))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["contract"] = ["The stored contract PDF could not be loaded. Ask the vendor to re-upload it."]
            });
        }

        return new BookingContractFileDto(
            pdfBytes,
            $"booking-contract-{booking.Id:N}.pdf",
            "application/pdf");
    }

    private static bool IsValidPdf(byte[] bytes) =>
        bytes.Length >= 5
        && bytes[0] == (byte)'%'
        && bytes[1] == (byte)'P'
        && bytes[2] == (byte)'D'
        && bytes[3] == (byte)'F';
}
