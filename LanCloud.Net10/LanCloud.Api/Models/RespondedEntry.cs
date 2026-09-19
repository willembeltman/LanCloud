using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Models;

public record RespondedEntry(
    FileSystemEntry FileSystemEntry,
    HubEntryDto ShareEntryDto, 
    string ReadPath);
