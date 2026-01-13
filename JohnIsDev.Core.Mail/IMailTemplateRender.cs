using JohnIsDev.Core.Models.Responses;

namespace JohnIsDev.Core.Mail;

/// <summary>
/// Defines a contract for rendering mail templates with dynamic model data.
/// </summary>
public interface IMailTemplateRender
{
    /// <summary>
    /// Renders a mail template asynchronously using the provided template key and model.
    /// </summary>
    /// <typeparam name="T">The type of the model to be used for rendering the template.</typeparam>
    /// <param name="templateName">The unique identifier of the template to render.</param>
    /// <param name="model">The model data to be injected into the template.</param>
    /// <returns>A task that represents the asynchronous operation, containing the rendered template as a string.</returns>
    Task<ResponseData<string>> RenderAsync<T>(string templateName, T model);
}