using ZooFinder.Application.Common.Language.Constants;

namespace ZooFinder.Application.Features.Discussions.Rooms.Contracts;

public sealed record CreateDiscussionRequest(
    string InformationSource,
    string SourceItemId,
    string LanguageCode = LanguageCodes.English);
