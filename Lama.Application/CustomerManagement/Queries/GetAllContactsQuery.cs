using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Queries;

public record GetAllContactsQuery : IQuery<IEnumerable<ContactDto>>;

public class GetAllContactsQueryHandler : IQueryHandler<GetAllContactsQuery, IEnumerable<ContactDto>>
{
    private readonly IRepository<Contact> _contactRepository;

    public GetAllContactsQueryHandler(IRepository<Contact> contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<IEnumerable<ContactDto>> Handle(GetAllContactsQuery query, CancellationToken cancellationToken)
    {
        var contacts = await _contactRepository.GetAllAsync(cancellationToken);

        return contacts.Select(c => new ContactDto(
            c.Id,
            c.FirstName,
            c.LastName,
            c.Email.Value,
            c.PhoneNumber?.Value,
            c.JobTitle,
            c.CompanyId,
            c.OwnerId,
            c.LifecycleStage,
            c.CreatedAt,
            c.LastActivityAt
        ));
    }
}

public record ContactDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? JobTitle,
    Guid? CompanyId,
    Guid? OwnerId,
    string? LifecycleStage,
    DateTime CreatedAt,
    DateTime LastActivityAt
);
