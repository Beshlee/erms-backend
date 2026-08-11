using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Approvals;
using ERMS.Application.Services;
using ERMS.Application.Validators;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using Moq;

namespace ERMS.Tests.Services;

/// <summary>
/// ApprovalService — yönetici onay/red kuralları (FR-32, FR-34, FR-36, FR-37).
/// Gerçek ApproveRequestDtoValidator/RejectRequestDtoValidator kullanılıyor.
/// </summary>
public class ApprovalServiceTests
{
    private const int ManagerId = 20;
    private const int EmployeeId = 10;

    private static Mock<ICurrentUserService> CurrentUserMock(int userId = ManagerId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(c => c.UserId).Returns(userId);
        mock.SetupGet(c => c.Role).Returns(UserRole.Manager);
        return mock;
    }

    private static ApprovalService CreateSut(
        Mock<IRepository<Request>> requestRepo,
        Mock<IRepository<User>> userRepo,
        Mock<ICurrentUserService> currentUser,
        Mock<IUnitOfWork>? unitOfWork = null)
    {
        unitOfWork ??= new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new ApprovalService(
            requestRepo.Object,
            Mock.Of<IRepository<Approval>>(),
            Mock.Of<IRepository<RequestHistory>>(),
            userRepo.Object,
            Mock.Of<IRequestQueryRepository>(),
            currentUser.Object,
            unitOfWork.Object,
            new ApproveRequestDtoValidator(),
            new RejectRequestDtoValidator());
    }

    [Fact]
    public async Task ApproveAsync_OwnRequest_ThrowsForbidden()
    {
        // FR-36: kullanıcı kendi talebini onaylayamaz.
        var request = new Request { Id = 1, RequesterId = ManagerId, Status = RequestStatus.Pending, Title = "x", Description = "y" };
        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var sut = CreateSut(requestRepo, new Mock<IRepository<User>>(), CurrentUserMock());

        await Assert.ThrowsAsync<ForbiddenAppException>(() => sut.ApproveAsync(1, new ApproveRequestDto(null)));
    }

    [Fact]
    public async Task ApproveAsync_NotManagerOfRequester_ThrowsForbidden()
    {
        // FR-32: yalnızca kendisine bağlı personelin talebi üzerinde karar verilebilir.
        var request = new Request { Id = 1, RequesterId = EmployeeId, Status = RequestStatus.Pending, Title = "x", Description = "y" };
        var requester = new User { Id = EmployeeId, FirstName = "Ahmet", LastName = "Yılmaz", Email = "a@b.com", PasswordHash = "h", ManagerId = 999 };

        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var userRepo = new Mock<IRepository<User>>();
        userRepo.Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(requester);

        var sut = CreateSut(requestRepo, userRepo, CurrentUserMock());

        await Assert.ThrowsAsync<ForbiddenAppException>(() => sut.ApproveAsync(1, new ApproveRequestDto(null)));
    }

    [Fact]
    public async Task ApproveAsync_AlreadyDecidedRequest_ThrowsConflict()
    {
        // FR-37: zaten sonuçlanmış bir talep tekrar karara bağlanamaz.
        var request = new Request { Id = 1, RequesterId = EmployeeId, Status = RequestStatus.Approved, Title = "x", Description = "y" };
        var requester = new User { Id = EmployeeId, FirstName = "Ahmet", LastName = "Yılmaz", Email = "a@b.com", PasswordHash = "h", ManagerId = ManagerId };

        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var userRepo = new Mock<IRepository<User>>();
        userRepo.Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(requester);

        var sut = CreateSut(requestRepo, userRepo, CurrentUserMock());

        await Assert.ThrowsAsync<ConflictAppException>(() => sut.ApproveAsync(1, new ApproveRequestDto(null)));
    }

    [Fact]
    public async Task ApproveAsync_ValidApproval_UpdatesStatusAndReturnsResult()
    {
        var request = new Request { Id = 1, RequesterId = EmployeeId, Status = RequestStatus.Pending, Title = "x", Description = "y" };
        var requester = new User { Id = EmployeeId, FirstName = "Ahmet", LastName = "Yılmaz", Email = "a@b.com", PasswordHash = "h", ManagerId = ManagerId };
        var manager = new User { Id = ManagerId, FirstName = "Mehmet", LastName = "Kaya", Email = "m@b.com", PasswordHash = "h" };

        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var userRepo = new Mock<IRepository<User>>();
        userRepo.Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(requester);
        userRepo.Setup(r => r.GetByIdAsync(ManagerId, It.IsAny<CancellationToken>())).ReturnsAsync(manager);

        var sut = CreateSut(requestRepo, userRepo, CurrentUserMock());

        var result = await sut.ApproveAsync(1, new ApproveRequestDto("Uygun görüldü."));

        Assert.Equal("Approved", result.Status);
        Assert.Equal("Mehmet Kaya", result.DecidedBy);
        Assert.Equal(RequestStatus.Approved, request.Status);
        requestRepo.Verify(r => r.Update(request), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_EmptyComment_ThrowsValidation()
    {
        // FR-34: reddetme gerekçesi zorunlu — repository'e hiç ulaşılmadan reddedilmeli.
        var requestRepo = new Mock<IRepository<Request>>();
        var sut = CreateSut(requestRepo, new Mock<IRepository<User>>(), CurrentUserMock());

        await Assert.ThrowsAsync<ValidationAppException>(() => sut.RejectAsync(1, new RejectRequestDto("")));

        requestRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectAsync_ValidRejection_TransitionsToRejected()
    {
        var request = new Request { Id = 1, RequesterId = EmployeeId, Status = RequestStatus.Pending, Title = "x", Description = "y" };
        var requester = new User { Id = EmployeeId, FirstName = "Ahmet", LastName = "Yılmaz", Email = "a@b.com", PasswordHash = "h", ManagerId = ManagerId };
        var manager = new User { Id = ManagerId, FirstName = "Mehmet", LastName = "Kaya", Email = "m@b.com", PasswordHash = "h" };

        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var userRepo = new Mock<IRepository<User>>();
        userRepo.Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(requester);
        userRepo.Setup(r => r.GetByIdAsync(ManagerId, It.IsAny<CancellationToken>())).ReturnsAsync(manager);

        var sut = CreateSut(requestRepo, userRepo, CurrentUserMock());

        var result = await sut.RejectAsync(1, new RejectRequestDto("Bütçe uygun değil."));

        Assert.Equal("Rejected", result.Status);
        Assert.Equal(RequestStatus.Rejected, request.Status);
    }
}
