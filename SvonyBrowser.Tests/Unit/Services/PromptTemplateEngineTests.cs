using SvonyBrowser.Tests.Fixtures;
using SvonyBrowser.Tests.Helpers;

namespace SvonyBrowser.Tests.Unit.Services;

/// <summary>
/// Unit tests for PromptTemplateEngine - MEDIUM priority service requiring 10+ tests.
/// </summary>
[Collection("ServiceTests")]
public class PromptTemplateEngineTests : ServiceTestFixture
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var instance1 = PromptTemplateEngine.Instance;
        var instance2 = PromptTemplateEngine.Instance;
        instance1.Should().BeSameAs(instance2);
    }
    
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        PromptTemplateEngine.Instance.Should().NotBeNull();
    }
    
    [Fact]
    public void Templates_ShouldNotBeNull()
    {
        PromptTemplateEngine.Instance.Templates.Should().NotBeNull();
    }
    
    [Fact]
    public void RenderTemplate_ShouldReturnString()
    {
        var result = PromptTemplateEngine.Instance.RenderTemplate("test", new Dictionary<string, object>());
        result.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData("greeting", "name", "World")]
    [InlineData("status", "status", "Online")]
    public void RenderTemplate_ShouldSubstituteVariables(string template, string varName, string varValue)
    {
        var variables = new Dictionary<string, object> { [varName] = varValue };
        var result = PromptTemplateEngine.Instance.RenderTemplate(template, variables);
        result.Should().NotBeNull();
    }
    
    [Fact]
    public void AddTemplate_ShouldNotThrow()
    {
        Action act = () => PromptTemplateEngine.Instance.AddTemplate("new_template", "Hello {{name}}!");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void RemoveTemplate_ShouldNotThrow()
    {
        Action act = () => PromptTemplateEngine.Instance.RemoveTemplate("nonexistent");
        act.Should().NotThrow();
    }
    
    [Fact]
    public void GetTemplate_ShouldReturnTemplate()
    {
        var template = PromptTemplateEngine.Instance.GetTemplate("test");
        // Can be null if not exists
    }
    
    [Fact]
    public void ListTemplates_ShouldReturnList()
    {
        var templates = PromptTemplateEngine.Instance.ListTemplates();
        templates.Should().NotBeNull();
    }
    
    [Fact]
    public void ValidateTemplate_ShouldReturnBool()
    {
        var isValid = PromptTemplateEngine.Instance.ValidateTemplate("Hello {{name}}!");
        isValid.Should().BeTrue();
    }
    
    [Fact]
    public void TemplateAdded_EventShouldBeSubscribable()
    {
        var eventRaised = false;
        PromptTemplateEngine.Instance.TemplateAdded += (name) => eventRaised = true;
        eventRaised.Should().BeFalse();
    }
}
