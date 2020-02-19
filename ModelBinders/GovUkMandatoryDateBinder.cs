﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using System.Threading.Tasks;
using GovUkDesignSystem.Attributes.DataBinding;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GovUkDesignSystem.ModelBinders
{
    /// <summary>
    /// This model binder can be used to replace the default MVC model binder for a required date property. It will add
    /// validation messages to the model state inline with the GovUk Design System guidelines.
    /// This binder must be used alongside a GovUkDataBindingDateErrorTextAttribute attribute.
    /// </summary>
    public class GovUkMandatoryDateBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var errorText = bindingContext.ModelMetadata.ValidatorMetadata.OfType<GovUkDataBindingDateErrorTextAttribute>().SingleOrDefault();
            if (errorText == null)
            {
                throw new Exception("When using the GovUkMandatoryDateBinder you must also provide a GovUkDataBindingDateErrorTextAttribute attribute and ensure that you register GovUkDataBindingErrorTextProvider in your application's Startup.ConfigureServices method.");
            }
            var modelName = bindingContext.ModelName;
            var modelSuffixes = new[] { "day", "month", "year" };

            var modelValueDictionary = modelSuffixes.ToDictionary(m => m, m => bindingContext.ValueProvider.GetValue(modelName + "-" + m));

            // Ensure that a not empty value was sent to us in the request
            if (modelValueDictionary.All(r => r.Value == ValueProviderResult.None || string.IsNullOrEmpty(r.Value.FirstValue))) 
            {
                return Task.CompletedTask;
            }

            var errors = new Dictionary<string, DateError>();
            var values = new Dictionary<string, int>();
            foreach (var valuePair in modelValueDictionary)
            {
                bindingContext.ModelState.SetModelValue(modelName + "-" + valuePair.Key, valuePair.Value);
                bindingContext.ModelState.MarkFieldValid(modelName + "-" + valuePair.Key);
                if (string.IsNullOrEmpty(valuePair.Value.FirstValue))
                {
                    errors.Add(valuePair.Key, DateError.ValueMissing);
                    continue;
                }

                if (!Int32.TryParse(valuePair.Value.FirstValue, out var value))
                {
                    errors.Add(valuePair.Key, DateError.ValueNotInt);
                    continue;
                }
                values.Add(valuePair.Key, value);
            }

            if (errors.Count != 0)
            {
                var errorMessage = errors.ContainsValue(DateError.ValueNotInt)
                    ? $"Enter a real {errorText.NameWithinSentence}"
                    : $"{errorText.NameAtStartOfSentence} does not include a {string.Join(" or a ", errors.Select(p => p.Key))}";

                bindingContext.ModelState.TryAddModelError(modelName, errorMessage);
                return Task.CompletedTask;
            }

            values.TryGetValue("day", out var day);
            values.TryGetValue("day", out var month);
            values.TryGetValue("day", out var year);
            try
            {
                bindingContext.ModelState.SetModelValue(modelName, new ValueProviderResult(new DateTime(year, month, day).ToLongDateString()));
                bindingContext.Result = ModelBindingResult.Success(new DateTime(year, month, day));
            }
            catch
            {
                bindingContext.ModelState.TryAddModelError(modelName, $"Enter a real {errorText.NameWithinSentence}");
            }
            return Task.CompletedTask;
        }

        private enum DateError
        {
            ValueMissing = 0,
            ValueNotInt = 1
        }
    }
}
