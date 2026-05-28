using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.SignBookingContract;

public class SignBookingContractCommandHandler : IRequestHandler<SignBookingContractCommand, SignBookingContractResult>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IBookingContractRepository _contractRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public SignBookingContractCommandHandler(
        IVendorBookingRepository bookingRepository,
        IBookingContractRepository contractRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingEventRepository eventRepository,
        IAuditLogRepository auditLogRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _contractRepository = contractRepository;
        _organizerRepository = organizerRepository;
        _eventRepository = eventRepository;
        _auditLogRepository = auditLogRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SignBookingContractResult> Handle(
        SignBookingContractCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException("Booking", request.BookingId);
        }

        var organizer = await _organizerRepository.GetOrganizerAsync(
            booking.EventId,
            request.UserId,
            cancellationToken);
        if (organizer is null || organizer.PermissionLevel == PermissionLevel.Viewer)
        {
            throw new ForbiddenAccessException("You do not have permission to sign contracts for this event.");
        }

        if (string.IsNullOrWhiteSpace(request.SignerName))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "signerName", new[] { "Signature name is required." } }
            });
        }

        var contract = await _contractRepository.GetByBookingIdAsync(booking.Id, cancellationToken);
        var contractUrl = request.ContractFileUrl
            ?? contract?.ContractFileUrl
            ?? $"https://contracts.mywedding.lk/bookings/{booking.Id}/standard.pdf";

        if (contract is null)
        {
            contract = new BookingContract
            {
                Id = booking.Id,
                ContractFileUrl = contractUrl,
                CreatedAt = DateTime.UtcNow
            };
            await _contractRepository.AddAsync(contract, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.ContractFileUrl))
        {
            contract.ContractFileUrl = request.ContractFileUrl;
        }

        var signedAt = DateTime.UtcNow;
        var pdfHash = ComputePdfHash(booking.Id, contractUrl, request.SignerName, request.UserId, signedAt);

        contract.ClientSignedAt = signedAt;
        booking.Status = BookingStatus.ContractSigned;

        var metadata = JsonSerializer.Serialize(new
        {
            bookingId = booking.Id,
            eventId = booking.EventId,
            signerName = request.SignerName.Trim(),
            clientIpAddress = request.ClientIpAddress,
            firebaseUid = request.UserId,
            pdfContentHash = pdfHash,
            contractFileUrl = contractUrl,
            signedAtUtc = signedAt
        });

        var auditItem = new AuditLogItem(
            Guid.NewGuid(),
            booking.EventId,
            request.UserId,
            "ContractSigned",
            $"{request.SignerName.Trim()} electronically signed the vendor contract (ref {booking.Id}).",
            metadata,
            signedAt);

        await _auditLogRepository.AddAsync(auditItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(booking.EventId, cancellationToken);

        var notifyUserIds = new List<string> { request.UserId, booking.BookedById };
        if (!string.IsNullOrEmpty(weddingEvent?.ManagingPlannerId))
        {
            notifyUserIds.Add(weddingEvent.ManagingPlannerId);
        }

        await _notificationService.NotifyContractSignedAsync(
            notifyUserIds.Distinct(),
            new
            {
                bookingId = booking.Id,
                eventId = booking.EventId,
                signerName = request.SignerName.Trim(),
                signedAtUtc = signedAt,
                message = "Vendor contract signed successfully."
            },
            cancellationToken);

        return new SignBookingContractResult
        {
            BookingId = booking.Id,
            EventId = booking.EventId,
            SignedAtUtc = signedAt,
            PdfContentHash = pdfHash,
            Status = booking.Status.ToString()
        };
    }

    private static string ComputePdfHash(
        Guid bookingId,
        string contractUrl,
        string signerName,
        string firebaseUid,
        DateTime signedAtUtc)
    {
        var payload = $"{bookingId}|{contractUrl}|{signerName}|{firebaseUid}|{signedAtUtc:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
