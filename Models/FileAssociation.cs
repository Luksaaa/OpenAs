namespace OpenAs.Models;

public sealed record FileAssociation(
    string Extension,
    FileFormat Format,
    string ProgId,
    string? PreviousProgId,
    string? IconLocation);
