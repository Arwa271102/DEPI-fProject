using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Matching;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class MatchingService : IMatchingService
{
    private const decimal MinimumSuggestedCompatibility = 60m;
    private readonly SakanakDbContext _dbContext;

    public MatchingService(SakanakDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<decimal>> CalculateCompatibilityAsync(int studentAId, int studentBId)
    {
        if (studentAId == studentBId)
        {
            return Result<decimal>.Success(100);
        }

        var students = await StudentQuery()
            .Where(item => item.StudentId == studentAId || item.StudentId == studentBId)
            .ToListAsync();

        var studentA = students.FirstOrDefault(item => item.StudentId == studentAId);
        var studentB = students.FirstOrDefault(item => item.StudentId == studentBId);
        if (studentA is null || studentB is null)
        {
            return Result<decimal>.Failure("Student was not found.");
        }

        return Result<decimal>.Success(CalculateCompatibility(studentA.Questionnaire, studentB.Questionnaire));
    }

    public async Task<Result<List<RoommateMatchDto>>> GetCompatibleStudentsForApartmentAsync(int studentId, int apartmentId)
    {
        var canMatch = await ValidateCanUseMatchingAsync(studentId, apartmentId);
        if (!canMatch.Succeeded)
        {
            return Result<List<RoommateMatchDto>>.Failure(canMatch.Errors);
        }

        var candidates = await _dbContext.Bookings
            .Include(item => item.Student).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Student).ThenInclude(item => item.Questionnaire)
            .Where(item =>
                item.ApartmentId == apartmentId &&
                item.StudentId != studentId &&
                item.Status == BookingStatus.Accepted &&
                item.Student.ApartmentGroupId == null)
            .Select(item => item.Student)
            .Distinct()
            .ToListAsync();

        var requester = await StudentQuery().FirstAsync(item => item.StudentId == studentId);
        var results = candidates
            .Select(candidate => MapMatch(candidate, CalculateCompatibility(requester.Questionnaire, candidate.Questionnaire), apartmentId))
            .OrderByDescending(item => item.CompatibilityScore)
            .ToList();

        return Result<List<RoommateMatchDto>>.Success(results);
    }

    public async Task<Result<List<RoommateMatchDto>>> GetApartmentGroupMembersAsync(int apartmentId)
    {
        var group = await GroupQuery().FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);
        if (group is null)
        {
            return Result<List<RoommateMatchDto>>.Success(new List<RoommateMatchDto>());
        }

        var members = group.Students.Select(student => MapMatch(student, 0, apartmentId)).ToList();
        return Result<List<RoommateMatchDto>>.Success(members);
    }

    public async Task<Result<List<RoommateMatchDto>>> GetGroupCompatibilityForStudentAsync(int studentId, int apartmentId)
    {
        var requester = await StudentQuery().FirstOrDefaultAsync(item => item.StudentId == studentId);
        if (requester is null)
        {
            return Result<List<RoommateMatchDto>>.Failure("Student profile was not found.");
        }

        var group = await GroupQuery().FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);
        if (group is null)
        {
            return Result<List<RoommateMatchDto>>.Success(new List<RoommateMatchDto>());
        }

        var members = group.Students
            .Where(item => item.StudentId != studentId)
            .Select(student => MapMatch(student, CalculateCompatibility(requester.Questionnaire, student.Questionnaire), apartmentId))
            .ToList();

        return Result<List<RoommateMatchDto>>.Success(members);
    }

    public async Task<Result<ApartmentGroupDto>> GetMyGroupAsync(int studentId)
    {
        var student = await StudentQuery()
            .Include(item => item.ApartmentGroup!).ThenInclude(item => item.Apartment)
            .Include(item => item.ApartmentGroup!).ThenInclude(item => item.Students).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.ApartmentGroup!).ThenInclude(item => item.Students).ThenInclude(item => item.Questionnaire)
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (student?.ApartmentGroup is null)
        {
            return Result<ApartmentGroupDto>.Failure("You are not currently in an apartment group.");
        }

        var hasActiveContract = await HasActiveContractAsync(studentId);
        return Result<ApartmentGroupDto>.Success(MapApartmentGroup(student.ApartmentGroup, student, canLeave: !hasActiveContract));
    }

    public async Task<Result> LeaveGroupAsync(int groupId, int studentId)
    {
        if (await HasActiveContractAsync(studentId))
        {
            return Result.Failure("You cannot leave a group after your contract is active. Please contact admin.");
        }

        var group = await GroupQuery().FirstOrDefaultAsync(item => item.GroupId == groupId);
        var student = await _dbContext.Students.FirstOrDefaultAsync(item => item.StudentId == studentId && item.ApartmentGroupId == groupId);
        if (group is null || student is null)
        {
            return Result.Failure("You are not in this group.");
        }

        student.ApartmentGroupId = null;
        if (group.Students.Count <= 1)
        {
            _dbContext.ApartmentGroups.Remove(group);
        }
        else
        {
            group.GroupStatus = GroupStatus.Open;
        }

        await _dbContext.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<Student> StudentQuery()
        => _dbContext.Students
            .Include(item => item.ApplicationUser)
            .Include(item => item.Questionnaire);

    private IQueryable<ApartmentGroup> GroupQuery()
        => _dbContext.ApartmentGroups
            .Include(item => item.Apartment)
            .Include(item => item.Students).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Students).ThenInclude(item => item.Questionnaire);

    private async Task<Result> ValidateCanUseMatchingAsync(int studentId, int apartmentId)
    {
        var student = await StudentQuery().FirstOrDefaultAsync(item => item.StudentId == studentId);
        if (student is null)
        {
            return Result.Failure("Student profile was not found.");
        }

        if (student.Questionnaire is null)
        {
            return Result.Failure("Complete your lifestyle profile to find compatible roommates.");
        }

        var hasAcceptedBooking = await _dbContext.Bookings.AnyAsync(item =>
            item.StudentId == studentId &&
            item.ApartmentId == apartmentId &&
            item.Status == BookingStatus.Accepted);

        return hasAcceptedBooking
            ? Result.Success()
            : Result.Failure("You need an accepted booking for this apartment before forming a group.");
    }

    private async Task<bool> HasActiveContractAsync(int studentId)
        => await _dbContext.Contracts.AnyAsync(item => item.StudentId == studentId && item.Status == ContractStatus.Active);

    private static decimal CalculateCompatibility(LifestyleQuestionnaire? first, LifestyleQuestionnaire? second)
    {
        if (first is null || second is null)
        {
            return 50;
        }

        if (GenderPreferenceConflicts(first.GenderPreference, second.GenderPreference))
        {
            return 0;
        }

        var sleep = ScoreSleep(first.SleepSchedule, second.SleepSchedule);
        var smoker = first.IsSmoker == second.IsSmoker ? 100m : 30m;
        var hygiene = Math.Max(0, 100 - Math.Abs(first.HygieneLevel - second.HygieneLevel) * 20);
        var study = first.StudyHabits == second.StudyHabits ? 100m : IsFlexibleStudy(first.StudyHabits) || IsFlexibleStudy(second.StudyHabits) ? 50m : 50m;
        var social = ScoreSocial(first.SocialPreference, second.SocialPreference);

        return Math.Round((sleep * .25m) + (smoker * .25m) + (hygiene * .20m) + (study * .15m) + (social * .15m), 1);
    }

    private static bool GenderPreferenceConflicts(GenderPreference first, GenderPreference second)
        => (first == GenderPreference.Male && second == GenderPreference.Female) ||
           (first == GenderPreference.Female && second == GenderPreference.Male);

    private static decimal ScoreSleep(SleepSchedule first, SleepSchedule second)
        => first == second ? 100 : first == SleepSchedule.Flexible || second == SleepSchedule.Flexible ? 75 : 20;

    private static bool IsFlexibleStudy(StudyHabits value)
        => value is StudyHabits.Flexible or StudyHabits.Mixed;

    private static decimal ScoreSocial(SocialPreference first, SocialPreference second)
    {
        if (first == second) return 100;
        if (first is SocialPreference.Moderate or SocialPreference.Ambivert || second is SocialPreference.Moderate or SocialPreference.Ambivert) return 70;
        return 30;
    }

    private static RoommateMatchDto MapMatch(Student student, decimal score, int apartmentId)
        => new()
        {
            StudentId = student.StudentId,
            Name = student.ApplicationUser.Name,
            University = student.University,
            Faculty = student.Faculty,
            CompatibilityScore = score,
            LifestyleSummary = BuildLifestyleSummary(student.Questionnaire),
            IsInSameApartment = true
        };

    private static GroupSuggestionDto MapGroupSuggestion(ApartmentGroup group, Student requester)
    {
        var members = group.Students.Select(member => MapStudentBasic(member, CalculateCompatibility(requester.Questionnaire, member.Questionnaire))).ToList();
        var average = members.Count == 0 ? 100 : Math.Round(members.Average(item => item.CompatibilityScore), 1);
        return new GroupSuggestionDto
        {
            GroupId = group.GroupId,
            GroupName = string.IsNullOrWhiteSpace(group.GroupName) ? $"Group #{group.GroupId}" : group.GroupName,
            ApartmentAddress = group.Apartment.Address,
            Members = members,
            AvailableSeats = Math.Max(0, group.MaxMembers - group.Students.Count),
            AverageCompatibilityScore = average,
            CanJoin = group.GroupStatus == GroupStatus.Open && group.Students.Count < group.MaxMembers
        };
    }

    private static ApartmentGroupDto MapApartmentGroup(ApartmentGroup group, Student viewer, bool canLeave)
    {
        var members = group.Students.Select(member => MapStudentBasic(member, member.StudentId == viewer.StudentId ? 100 : CalculateCompatibility(viewer.Questionnaire, member.Questionnaire))).ToList();
        return new ApartmentGroupDto
        {
            GroupId = group.GroupId,
            ApartmentId = group.ApartmentId,
            GroupName = string.IsNullOrWhiteSpace(group.GroupName) ? $"Group #{group.GroupId}" : group.GroupName,
            Status = group.GroupStatus.ToString(),
            ApartmentAddress = group.Apartment.Address,
            MaxMembers = group.MaxMembers,
            AvailableSeats = Math.Max(0, group.MaxMembers - group.Students.Count),
            AverageCompatibilityScore = members.Count <= 1 ? 100 : Math.Round(members.Where(item => item.StudentId != viewer.StudentId).Average(item => item.CompatibilityScore), 1),
            Members = members,
            CanLeave = canLeave
        };
    }

    private static StudentBasicDto MapStudentBasic(Student student, decimal compatibilityScore)
        => new()
        {
            StudentId = student.StudentId,
            Name = student.ApplicationUser.Name,
            University = student.University,
            Faculty = student.Faculty,
            CompatibilityScore = compatibilityScore,
            LifestyleSummary = BuildLifestyleSummary(student.Questionnaire)
        };

    private static string BuildLifestyleSummary(LifestyleQuestionnaire? questionnaire)
        => questionnaire is null
            ? "Lifestyle profile incomplete"
            : $"{(questionnaire.IsSmoker ? "Smoker" : "Non-smoker")}, {questionnaire.SleepSchedule}, Hygiene {questionnaire.HygieneLevel}/5";

    public async Task<Result<List<RoommateDiscoveryDto>>> FindCompatibleStudentsAsync(int studentId)
    {
        var requester = await StudentQuery().FirstOrDefaultAsync(item => item.StudentId == studentId);
        if (requester?.Questionnaire is null)
        {
            return Result<List<RoommateDiscoveryDto>>.Failure("Complete your questionnaire first.");
        }

        var otherStudents = await StudentQuery()
            .Include(item => item.Bookings).ThenInclude(item => item.Apartment)
            .Include(item => item.Contracts).ThenInclude(item => item.Apartment)
            .Include(item => item.ApartmentGroup).ThenInclude(item => item!.Students)
            .Include(item => item.Media)
            .Where(item => item.StudentId != studentId && item.Questionnaire != null)
            .ToListAsync();

        var compatibleStudents = new List<RoommateDiscoveryDto>();

        foreach (var other in otherStudents)
        {
            if (GenderPreferenceConflicts(requester.Questionnaire.GenderPreference, other.Questionnaire!.GenderPreference))
            {
                continue;
            }

            var score = CalculateCompatibility(requester.Questionnaire, other.Questionnaire);
            if (score < 60)
            {
                continue;
            }

            var hasActiveContract = other.Contracts.FirstOrDefault(item => item.Status == ContractStatus.Active);
            var acceptedBooking = other.Bookings.FirstOrDefault(item => item.Status == BookingStatus.Accepted);

            var apartment = hasActiveContract?.Apartment ?? acceptedBooking?.Apartment;
            var housingStatus = apartment is not null ? StudentHousingStatus.HasApartment : StudentHousingStatus.Searching;

            compatibleStudents.Add(new RoommateDiscoveryDto
            {
                StudentId = other.StudentId,
                Name = other.ApplicationUser.Name,
                University = other.University,
                Faculty = other.Faculty,
                ProfilePhotoUrl = other.Media.FirstOrDefault(m => m.Type == MediaType.Image)?.Url,
                CompatibilityScore = score,
                CompatibilityReasons = BuildCompatibilityReasons(requester.Questionnaire, other.Questionnaire),
                HousingStatus = housingStatus,
                ApartmentId = apartment?.ApartmentId,
                ApartmentAddress = apartment?.Address,
                ApartmentCity = apartment?.City,
                ApartmentPrice = apartment?.PricePerMonth,
                AvailableSeats = apartment != null ? Math.Max(0, apartment.TotalSeats - (other.ApartmentGroup?.Students.Count ?? 1)) : null
            });
        }

        return Result<List<RoommateDiscoveryDto>>.Success(compatibleStudents.OrderByDescending(item => item.CompatibilityScore).Take(30).ToList());
    }

    private static List<string> BuildCompatibilityReasons(LifestyleQuestionnaire first, LifestyleQuestionnaire second)
    {
        var reasons = new List<string>();

        if (first.IsSmoker == second.IsSmoker)
        {
            reasons.Add(first.IsSmoker ? "🚬 Both are smokers" : "🚭 Both are non-smokers");
        }

        if (first.SleepSchedule == second.SleepSchedule && first.SleepSchedule != SleepSchedule.Flexible)
        {
            var icon = first.SleepSchedule == SleepSchedule.NightOwl ? "🦉" : "🌅";
            reasons.Add($"{icon} Both are {first.SleepSchedule.ToString().ToLower()}s");
        }

        if (first.StudyHabits == second.StudyHabits && !IsFlexibleStudy(first.StudyHabits))
        {
            reasons.Add("📚 Both prefer studying at " + first.StudyHabits.ToString().ToLower());
        }

        if (first.SocialPreference == second.SocialPreference && first.SocialPreference != SocialPreference.Moderate)
        {
            var icon = first.SocialPreference == SocialPreference.Quiet ? "🤫" : "🎉";
            reasons.Add($"{icon} Both prefer {first.SocialPreference.ToString().ToLower()} environments");
        }

        if (Math.Abs(first.HygieneLevel - second.HygieneLevel) <= 1)
        {
            reasons.Add("✨ Similar hygiene standards");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("🤝 Good overall compatibility");
        }

        return reasons;
    }
}
