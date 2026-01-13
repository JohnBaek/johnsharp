using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Responses;
using Microsoft.Extensions.Logging;
using Scriban;

namespace JohnIsDev.Core.Mail;

/// <summary>
/// Provides functionality for rendering mail templates using the Scriban templating engine.
/// Implements the <see cref="IMailTemplateRender"/> interface to support dynamic model-based template rendering.
/// </summary>
public class ScribanTemplateRenderer(ILogger<ScribanTemplateRenderer> logger) : IMailTemplateRender
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="templateName"></param>
    /// <param name="model"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<ResponseData<string>> RenderAsync<T>(string templateName, T model)
    {
        try
        {
            // Get template content
            string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", $"{templateName}.scriban");
            string templateContent = await File.ReadAllTextAsync(templatePath);

            // Parse template content
            Template? template = Template.Parse(templateContent);

            // If template is null, return error response
            if(template == null)
                return new ResponseData<string>(EnumResponseResult.Error, "", "");

            return new ResponseData<string>(EnumResponseResult.Success, "", await template.RenderAsync(model));
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return new ResponseData<string>(EnumResponseResult.Error, "", "");
        }
    }
}
