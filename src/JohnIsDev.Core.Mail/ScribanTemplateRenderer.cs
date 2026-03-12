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
    /// Renders a template using the specified model and returns the rendered output as a string.
    /// </summary>
    /// <typeparam name="T">The type of the model used for rendering the template.</typeparam>
    /// <param name="templateContent">The template content to be rendered.</param>
    /// <param name="model">The model object containing data to populate the template.</param>
    /// <returns>A <see cref="ResponseData{string}"/> object, containing the rendered template output or an error in case of failure.</returns>
    public async Task<ResponseData<string>> RenderAsync<T>(string templateContent, T model)
    {
        try
        {
            // Parse template content
            Template? template = Template.Parse(templateContent);

            // If template is null, return error response
            if(template == null)
                return new ResponseData<string>(EnumResponseResult.Error, "", "");

            string? parsed = await template.RenderAsync(model);
            return new ResponseData<string>(EnumResponseResult.Success, "","",parsed);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return new ResponseData<string>(EnumResponseResult.Error, "", "");
        }
    }
}
