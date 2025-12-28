using System.ComponentModel;
using System.Threading.Tasks;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    public enum IndustryType
    {
        Technology,
        Healthcare,
        Finance,
        Retail,
        Manufacturing,
        Education,
        Entertainment,
        RealEstate
    }

    public enum FundingStage
    {
        PreSeed,
        Seed,
        SeriesA,
        SeriesB,
        SeriesC,
        Ipo
    }

    public enum MarketStrategy
    {
        B2B,
        B2C,
        B2B2C,
        Marketplace,
        Saas,
        Freemium
    }

    [Description("Generate a business plan for a startup idea. Provide company details and get a structured business plan.")]
    public class BusinessPlanTool : VoiceToolBase<BusinessPlanTool.Args>
    {
        public record Args(
            [property: Description("The name of the company or startup")]
            string CompanyName,

            [property: Description("The industry sector the business operates in")]
            IndustryType Industry,

            [property: Description("Target funding amount in USD as a number (e.g., 500000 for $500k)")]
            double TargetFunding,

            [property: Description("Current funding stage of the company, or null if not yet determined")]
            FundingStage? FundingStage = null,

            [property: Description("Go-to-market strategy for the product, or null to use default")]
            MarketStrategy? Strategy = MarketStrategy.B2C,

            [property: Description("Number of team members as an integer, or null if unknown")]
            int? TeamSize = null,

            [property: Description("Expected annual revenue in first year as a number in USD, or null if not yet projected")]
            double? ProjectedRevenue = null,

            [property: Description("Brief description of the main product or service, or null if not provided")]
            string? ProductDescription = null
        );

        public override string Name => "generate_business_plan";

        public override Task<string> ExecuteAsync(Args args)
        {
            var fundingStageText = args.FundingStage?.ToString() ?? "not specified";
            var teamSizeText = args.TeamSize?.ToString() ?? "to be determined";
            var revenueText = args.ProjectedRevenue.HasValue
                ? $"${args.ProjectedRevenue.Value:N0}"
                : "projections pending";
            var productText = args.ProductDescription ?? "innovative solution in the space";

            var businessPlan = $@"
=== BUSINESS PLAN: {args.CompanyName.ToUpperInvariant()} ===

EXECUTIVE SUMMARY
-----------------
{args.CompanyName} is an exciting {args.Industry.ToString().ToLower()} startup
pursuing a {args.Strategy?.ToString() ?? "B2C"} market strategy.

COMPANY OVERVIEW
----------------
Company Name: {args.CompanyName}
Industry: {args.Industry}
Current Stage: {fundingStageText}
Team Size: {teamSizeText} employees

PRODUCT/SERVICE
---------------
{productText}

FINANCIAL PROJECTIONS
---------------------
Target Funding: ${args.TargetFunding:N0}
Projected Year 1 Revenue: {revenueText}
Go-to-Market Strategy: {args.Strategy?.ToString() ?? "B2C"}

INVESTMENT OPPORTUNITY
----------------------
{args.CompanyName} is seeking ${args.TargetFunding:N0} in {fundingStageText} funding
to accelerate growth in the {args.Industry.ToString().ToLower()} sector.

=== END OF BUSINESS PLAN ===
";

            return Task.FromResult(CreateSuccessResult(new
            {
                company = args.CompanyName,
                industry = args.Industry.ToString(),
                funding_target = args.TargetFunding,
                funding_stage = fundingStageText,
                strategy = args.Strategy?.ToString() ?? "B2C",
                team_size = teamSizeText,
                projected_revenue = revenueText,
                business_plan = businessPlan.Trim()
            }));
        }
    }
}
