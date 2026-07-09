using Sakanak.DAL.Repositories.Interfaces;

namespace Sakanak.DAL.UnitOfWork;

/// <summary>
/// Coordinates repository access and transaction management for the DAL.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IStudentRepository Students { get; }
    ILandlordRepository Landlords { get; }
    IAdminRepository Admins { get; }
    IApartmentRepository Apartments { get; }
    IApartmentGroupRepository ApartmentGroups { get; }
    IBookingRepository Bookings { get; }
    IContractRepository Contracts { get; }
    IPaymentRepository Payments { get; }
    ILifestyleQuestionnaireRepository LifestyleQuestionnaires { get; }
    IMediaRepository Media { get; }
    IRequestRepository Requests { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
