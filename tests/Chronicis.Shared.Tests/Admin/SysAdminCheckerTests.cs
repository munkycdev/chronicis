using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Admin;

namespace Chronicis.Shared.Tests.Admin;

[ExcludeFromCodeCoverage]
public class SysAdminCheckerTests
{
    // ────────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SysAdminChecker(null!));
    }

    [Fact]
    public void Constructor_NullAuth0UserIds_DoesNotThrow()
    {
        var options = new SysAdminOptions { Auth0UserIds = null!, Emails = [] };
        var checker = new SysAdminChecker(options);

        Assert.False(checker.IsSysAdmin("any-id", null));
    }

    [Fact]
    public void Constructor_NullEmails_DoesNotThrow()
    {
        var options = new SysAdminOptions { Auth0UserIds = [], Emails = null! };
        var checker = new SysAdminChecker(options);

        Assert.False(checker.IsSysAdmin("any-id", "any@email.com"));
    }

    // ────────────────────────────────────────────────────────────────
    //  IsSysAdmin — Auth0 user ID matching
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void IsSysAdmin_MatchingAuth0UserId_ReturnsTrue()
    {
        var checker = BuildChecker(
            auth0Ids: ["oauth2|discord|123456"],
            emails: []);

        Assert.True(checker.IsSysAdmin("oauth2|discord|123456", null));
    }

    [Fact]
    public void IsSysAdmin_Auth0UserId_IsCaseInsensitive()
    {
        var checker = BuildChecker(
            auth0Ids: ["OAuth2|Discord|123456"],
            emails: []);

        Assert.True(checker.IsSysAdmin("oauth2|discord|123456", null));
    }

    [Fact]
    public void IsSysAdmin_NonMatchingAuth0UserId_ReturnsFalse()
    {
        var checker = BuildChecker(
            auth0Ids: ["oauth2|discord|123456"],
            emails: []);

        Assert.False(checker.IsSysAdmin("oauth2|discord|999999", null));
    }

    [Fact]
    public void IsSysAdmin_EmptyAuth0UserId_ReturnsFalse()
    {
        var checker = BuildChecker(
            auth0Ids: ["oauth2|discord|123456"],
            emails: []);

        Assert.False(checker.IsSysAdmin(string.Empty, null));
    }

    // ────────────────────────────────────────────────────────────────
    //  IsSysAdmin — Email matching
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void IsSysAdmin_MatchingEmail_ReturnsTrue()
    {
        var checker = BuildChecker(
            auth0Ids: [],
            emails: ["admin@chronicis.app"]);

        Assert.True(checker.IsSysAdmin("some-unknown-id", "admin@chronicis.app"));
    }

    [Fact]
    public void IsSysAdmin_Email_IsCaseInsensitive()
    {
        var checker = BuildChecker(
            auth0Ids: [],
            emails: ["Admin@Chronicis.App"]);

        Assert.True(checker.IsSysAdmin("some-unknown-id", "admin@chronicis.app"));
    }

    [Fact]
    public void IsSysAdmin_NonMatchingEmail_ReturnsFalse()
    {
        var checker = BuildChecker(
            auth0Ids: [],
            emails: ["admin@chronicis.app"]);

        Assert.False(checker.IsSysAdmin("some-unknown-id", "other@chronicis.app"));
    }

    [Fact]
    public void IsSysAdmin_NullEmail_ReturnsFalse()
    {
        var checker = BuildChecker(
            auth0Ids: [],
            emails: ["admin@chronicis.app"]);

        Assert.False(checker.IsSysAdmin("some-unknown-id", null));
    }

    [Fact]
    public void IsSysAdmin_EmptyEmail_ReturnsFalse()
    {
        var checker = BuildChecker(
            auth0Ids: [],
            emails: ["admin@chronicis.app"]);

        Assert.False(checker.IsSysAdmin("some-unknown-id", string.Empty));
    }

    // ────────────────────────────────────────────────────────────────
    //  IsSysAdmin — Auth0 ID takes priority over email
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void IsSysAdmin_Auth0IdMatches_ReturnsTrueEvenWithNonMatchingEmail()
    {
        var checker = BuildChecker(
            auth0Ids: ["oauth2|discord|123456"],
            emails: ["admin@chronicis.app"]);

        Assert.True(checker.IsSysAdmin("oauth2|discord|123456", "nobody@example.com"));
    }

    // ────────────────────────────────────────────────────────────────
    //  IsSysAdmin — Empty configuration
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void IsSysAdmin_EmptyOptions_ReturnsFalse()
    {
        var checker = BuildChecker(auth0Ids: [], emails: []);

        Assert.False(checker.IsSysAdmin("oauth2|discord|123456", "admin@chronicis.app"));
    }

    // ────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────

    private static SysAdminChecker BuildChecker(
        IReadOnlyList<string> auth0Ids,
        IReadOnlyList<string> emails)
    {
        return new SysAdminChecker(new SysAdminOptions
        {
            Auth0UserIds = auth0Ids,
            Emails = emails
        });
    }
}
