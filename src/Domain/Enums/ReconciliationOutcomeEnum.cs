namespace KiotVietTool.Domain.Enums;

public enum ReconciliationOutcomeEnum
{
    Matched = 1,
    MissingDiscount = 2,
    WrongAmount = 3,
    UnexpectedDiscount = 4,
    NearBoundary = 5,
}
