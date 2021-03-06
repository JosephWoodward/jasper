﻿namespace JasperHttpTesting.Assertions
{
    // SAMPLE: StatusCodeAssertion
    public class StatusCodeAssertion : IScenarioAssertion
    {
        public int Expected { get; set; }

        public StatusCodeAssertion(int expected)
        {
            Expected = expected;
        }

        public void Assert(Scenario scenario, ScenarioAssertionException ex)
        {
            var statusCode = scenario.Context.Response.StatusCode;
            if (statusCode != Expected)
            {
                ex.Add($"Expected status code {Expected}, but was {statusCode}");

                ex.ShowActualBodyInErrorMessage(scenario);
            }
        }
    }
    // ENDSAMPLE
}