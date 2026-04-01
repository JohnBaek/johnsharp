using FluentAssertions;
using JohnIsDev.Core.Features.Utils;

namespace JohnIsDev.Core.Test.Features.Utils;

/// <summary>
/// Provides unit tests for the SessionKeyUtil class, ensuring functionality and integrity when
/// generating or handling session keys within the application.
/// </summary>
public class SessionKeyUtilTest
{
    /// <summary>
    /// Can Generate Session Key
    /// </summary>
    [Fact]
    public void CanGenerateSessionKey()
    {
        // Arrange
        // Act
        string generatedKey1 = SessionKeyUtil.Generate();
        
        // Assert
        generatedKey1.Should().NotBeEmpty();
    }
    
    /// <summary>
    /// Can Generate Unique Session Key
    /// </summary>
    [Fact]
    public void Can_Generate_Unique_Session_Key()
    {
        // Arrange
        // Act
        string generatedKey1 = SessionKeyUtil.Generate();
        string generatedKey2 = SessionKeyUtil.Generate();
        
        // Assert
        generatedKey1.Should().NotBe(generatedKey2).And.NotBeEmpty();
    }
}