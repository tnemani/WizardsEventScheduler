public class GameTemplateConfigDto
{
    public string GameId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DefaultMaxCapacity { get; set; }
    public int MinPlayersToStart { get; set; }
    public List<GameFormatConfigDto> Formats { get; set; } = new();
}