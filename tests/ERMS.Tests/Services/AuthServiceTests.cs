using System.Linq.Expressions;
using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Auth;
using ERMS.Application.Services;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using ERMS.Infrastructure.Authentication;
using Moq;

namespace ERMS.Tests.Services;

/// <summary>
/// FR-01/FR-06 — giriş ve pasif kullanıcı reddi. AuthService.LoginAsync, yanlış parola ile
/// pasif kullanıcı için AYNI hata mesajını dönmeli (US-01) — bu güvenlik kuralı özellikle test edilir.
/// RefreshAsync/RevokeRefreshTokenAsync — bonus (refresh token akışı).
/// </summary>
public class AuthServiceTests
{
    private static User CreateActiveUser(string passwordHash = "hashed-password") => new()
    {
        Id = 1,
        FirstName = "Ahmet",
        LastName = "Yılmaz",
        Email = "ahmet@sirket.com",
        PasswordHash = passwordHash,
        Role = UserRole.Employee,
        IsActive = true
    };

    private static Mock<IRepository<User>> CreateUserRepositoryMock(User? user)
    {
        var repo = new Mock<IRepository<User>>();
        repo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return repo;
    }

    // Gerçek (mock'lanmamış) implementasyon kullanılıyor: saf/deterministik bir fonksiyon
    // olduğu için davranışını taklit etmeye gerek yok, ayrıca aşağıdaki testlerde
    // "DB'ye yazılan değer gerçekten hash'lenmiş mi" diye doğrudan doğrulayabilmek için gerekli.
    private static readonly IRefreshTokenHasher RefreshTokenHasher = new RefreshTokenHasher();

    private static AuthService CreateSut(
        Mock<IRepository<User>> userRepo,
        IPasswordHasher? passwordHasher = null,
        IJwtTokenService? jwtTokenService = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new AuthService(
            userRepo.Object,
            passwordHasher ?? Mock.Of<IPasswordHasher>(),
            jwtTokenService ?? Mock.Of<IJwtTokenService>(),
            RefreshTokenHasher,
            unitOfWork.Object);
    }

    private static JwtResult CreateJwtResult(string token = "token-123") => new(
        token,
        DateTime.UtcNow.AddHours(1),
        $"refresh-{token}",
        DateTime.UtcNow.AddDays(7));

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUserSummary()
    {
        var user = CreateActiveUser();
        var userRepo = CreateUserRepositoryMock(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(p => p.Verify("Passw0rd!", user.PasswordHash)).Returns(true);

        var jwt = CreateJwtResult();
        var jwtService = new Mock<IJwtTokenService>();
        jwtService.Setup(j => j.GenerateToken(user)).Returns(jwt);

        var sut = CreateSut(userRepo, passwordHasher.Object, jwtService.Object);

        var result = await sut.LoginAsync(new LoginRequestDto(user.Email, "Passw0rd!"));

        Assert.Equal(jwt.Token, result.Token);
        Assert.Equal(jwt.ExpiresAtUtc, result.ExpiresAt);
        Assert.Equal(jwt.RefreshToken, result.RefreshToken);
        Assert.Equal("Ahmet Yılmaz", result.User.FullName);
        Assert.Equal("Employee", result.User.Role);

        // Kullanıcıya kalıcı olarak yazılan değer, dönen düz metin token'ın kendisi DEĞİL,
        // hash'i olmalı (bkz. IRefreshTokenHasher) — DB'de asla düz metin saklanmaz.
        Assert.Equal(RefreshTokenHasher.Hash(jwt.RefreshToken), user.RefreshToken);
        Assert.NotEqual(jwt.RefreshToken, user.RefreshToken);
        userRepo.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var user = CreateActiveUser();
        var userRepo = CreateUserRepositoryMock(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var sut = CreateSut(userRepo, passwordHasher.Object);

        var ex = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => sut.LoginAsync(new LoginRequestDto(user.Email, "yanlis-parola")));

        Assert.Equal("INVALID_CREDENTIALS", ex.Code);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedWithSameMessageAsWrongPassword()
    {
        // US-01: "bu e-posta var ama hesap pasif" bilgisi dışarı sızmamalı — pasif kullanıcı
        // ve yanlış parola AYNI hata mesajını/koduyla reddedilmeli.
        var inactiveUser = CreateActiveUser();
        inactiveUser.IsActive = false;
        var userRepo = CreateUserRepositoryMock(inactiveUser);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var sut = CreateSut(userRepo, passwordHasher.Object);

        var inactiveEx = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => sut.LoginAsync(new LoginRequestDto(inactiveUser.Email, "Passw0rd!")));

        var wrongPasswordSut = CreateSut(
            CreateUserRepositoryMock(CreateActiveUser()),
            Mock.Of<IPasswordHasher>(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()) == false));

        var wrongPasswordEx = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => wrongPasswordSut.LoginAsync(new LoginRequestDto("ahmet@sirket.com", "yanlis-parola")));

        Assert.Equal(wrongPasswordEx.Message, inactiveEx.Message);
        Assert.Equal(wrongPasswordEx.Code, inactiveEx.Code);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized()
    {
        var userRepo = CreateUserRepositoryMock(null);
        var sut = CreateSut(userRepo);

        await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => sut.LoginAsync(new LoginRequestDto("olmayan@sirket.com", "Passw0rd!")));
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesTokensAndPersists()
    {
        var user = CreateActiveUser();
        user.RefreshToken = "eski-refresh-token";
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(3);
        var userRepo = CreateUserRepositoryMock(user);

        var newJwt = CreateJwtResult("token-yeni");
        var jwtService = new Mock<IJwtTokenService>();
        jwtService.Setup(j => j.GenerateToken(user)).Returns(newJwt);

        var sut = CreateSut(userRepo, jwtTokenService: jwtService.Object);

        var result = await sut.RefreshAsync(new RefreshTokenRequestDto("eski-refresh-token"));

        Assert.Equal(newJwt.Token, result.Token);
        Assert.Equal(newJwt.RefreshToken, result.RefreshToken);
        // Rotation: eski refresh token'ın hash'i artık kullanıcıda değil, yenisinin hash'iyle
        // değişti (DB'de düz metin hiçbir zaman saklanmaz).
        Assert.Equal(RefreshTokenHasher.Hash(newJwt.RefreshToken), user.RefreshToken);
        Assert.NotEqual("eski-refresh-token", user.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ThrowsUnauthorized()
    {
        var user = CreateActiveUser();
        user.RefreshToken = "suresi-dolmus-token";
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1);
        var userRepo = CreateUserRepositoryMock(user);

        var sut = CreateSut(userRepo);

        var ex = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => sut.RefreshAsync(new RefreshTokenRequestDto("suresi-dolmus-token")));

        Assert.Equal("INVALID_REFRESH_TOKEN", ex.Code);
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_ThrowsUnauthorized()
    {
        var userRepo = CreateUserRepositoryMock(null);
        var sut = CreateSut(userRepo);

        await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => sut.RefreshAsync(new RefreshTokenRequestDto("hic-var-olmamis-token")));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ValidToken_ClearsItFromUser()
    {
        var user = CreateActiveUser();
        user.RefreshToken = "aktif-token";
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(3);
        var userRepo = CreateUserRepositoryMock(user);

        var sut = CreateSut(userRepo);

        await sut.RevokeRefreshTokenAsync(new RefreshTokenRequestDto("aktif-token"));

        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_UnknownToken_DoesNotThrow()
    {
        // Bilinmeyen bir token için sessizce başarılı sayılır (çıkış işlemi zaten
        // istenen sonuca ulaşmış demektir — ortada geçerli bir token yok).
        var userRepo = CreateUserRepositoryMock(null);
        var sut = CreateSut(userRepo);

        var exception = await Record.ExceptionAsync(
            () => sut.RevokeRefreshTokenAsync(new RefreshTokenRequestDto("bilinmeyen-token")));

        Assert.Null(exception);
    }
}
