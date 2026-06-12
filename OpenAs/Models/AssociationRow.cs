namespace OpenAs.Models;

public sealed class AssociationRow(FileAssociation association)
{
    public FileAssociation Association { get; } = association;
    public string Extension => Association.Extension;
    public string OpensAs => Association.Format.DisplayName;
    public string IconMode => IsCustomIcon ? "Custom icon" : "Default type icon";
    public string Behavior => $"Opens as {Association.Format.Extension}";

    private bool IsCustomIcon =>
        !string.IsNullOrWhiteSpace(Association.IconLocation)
        && Association.IconLocation.Contains(@"\OpenAs\Icons\", StringComparison.OrdinalIgnoreCase);
}
