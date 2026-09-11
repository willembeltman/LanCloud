using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Models;

public record Entry(
    FileSystemEntry FileSystemEntry,
    ShareEntryDto ShareEntryDto, 
    string ReadPath);
