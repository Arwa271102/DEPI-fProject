using Microsoft.EntityFrameworkCore.Storage;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Implementations;
using Sakanak.DAL.Repositories.Interfaces;

namespace Sakanak.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly SakanakDbContext _context;
    private IDbContextTransaction? _transaction;
    private IStudentRepository? _students;
    private ILandlordRepository? _landlords;
    private IAdminRepository? _admins;
    private IApartmentRepository? _apartments;
    private IApartmentGroupRepository? _apartmentGroups;
    private IBookingRepository? _bookings;
    private IContractRepository? _contracts;
    private IPaymentRepository? _payments;
    private ILifestyleQuestionnaireRepository? _lifestyleQuestionnaires;
    private IMediaRepository? _media;
    private IRequestRepository? _requests;

    public UnitOfWork(SakanakDbContext context)
    {
        _context = context;
    }

    public IStudentRepository Students => _students ??= new StudentRepository(_context);
    public ILandlordRepository Landlords => _landlords ??= new LandlordRepository(_context);
    public IAdminRepository Admins => _admins ??= new AdminRepository(_context);
    public IApartmentRepository Apartments => _apartments ??= new ApartmentRepository(_context);
    public IApartmentGroupRepository ApartmentGroups => _apartmentGroups ??= new ApartmentGroupRepository(_context);
    public IBookingRepository Bookings => _bookings ??= new BookingRepository(_context);
    public IContractRepository Contracts => _contracts ??= new ContractRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public ILifestyleQuestionnaireRepository LifestyleQuestionnaires => _lifestyleQuestionnaires ??= new LifestyleQuestionnaireRepository(_context);
    public IMediaRepository Media => _media ??= new MediaRepository(_context);
    public IRequestRepository Requests => _requests ??= new RequestRepository(_context);

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync()
    {
        _transaction ??= await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _context.SaveChangesAsync();
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
