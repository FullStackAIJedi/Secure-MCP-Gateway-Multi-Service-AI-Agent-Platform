namespace Microsoft.Extensions.Hosting.Observability
{
    public sealed class RedactionOptions
    {
        public const string SectionName = "Redaction";

        public bool AllowAllKeys { get; set; }
        public List<string> AllowedKeys { get; set; } = [];
        public List<string> BlockedValues { get; set; } = [];
    }
}
