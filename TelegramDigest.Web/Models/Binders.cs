using Microsoft.AspNetCore.Mvc.ModelBinding;
using TelegramDigest.Backend.Core;
using Host = TelegramDigest.HostHandler.Host;

namespace TelegramDigest.Web.Models;

public sealed class ChannelTgIdModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelName = bindingContext.ModelName;

        // Try to fetch the value of the argument by name
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;

        // Check if the argument value is null or empty
        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        try
        {
            var result = new ChannelTgId(value);
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError(modelName, ex.Message);
            return Task.CompletedTask;
        }
    }
}

public sealed class TemplateWithContentModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelName = bindingContext.ModelName;

        // Try to fetch the value of the argument by name
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;

        // Check if the argument value is null or empty
        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        try
        {
            var result = new TemplateWithContent(value);
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError(modelName, ex.Message);
            return Task.CompletedTask;
        }
    }
}

public sealed class HostModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelName = bindingContext.ModelName;

        // Try to fetch the value of the argument by name
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;

        // Check if the argument value is null or empty
        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        try
        {
            var result = new Host(value);
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError(modelName, ex.Message);
            return Task.CompletedTask;
        }
    }
}
