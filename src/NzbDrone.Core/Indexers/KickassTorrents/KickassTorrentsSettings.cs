using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.KickassTorrents
{
    public class KickassTorrentsSettingsValidator : AbstractValidator<KickassTorrentsSettings>
    {
        public KickassTorrentsSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
        }
    }

    public class KickassTorrentsSettings : IProviderConfig
    {
        private static readonly KickassTorrentsSettingsValidator validator = new KickassTorrentsSettingsValidator();

        public KickassTorrentsSettings()
        {
            BaseUrl = "http://kickass.to";
            VerifiedOnly = true;
        }

        [FieldDefinition(0, Label = "Website URL")]
        public String BaseUrl { get; set; }

        public Boolean VerifiedOnly { get; set; }

        public ValidationResult Validate()
        {
            return validator.Validate(this);
        }
    }
}