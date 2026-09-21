using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Models;

internal record RespondedEntry(
    FileSystemEntry FileSystemEntry,
    HubEntryDto ShareEntryDto, 
    string Path);
