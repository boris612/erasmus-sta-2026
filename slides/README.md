# Slides

This folder contains the exported slide decks for the lectures used in this repository:

- `T1: Unit Testing and Integration Testing in ASP.NET Core`
- `T2: Web API Integration Testing in Complex Scenarios`

These presentation materials were created for Erasmus+ `KA131 STA` teaching activities at the Faculty of Computer Science and Engineering, Ss. Cyril and Methodius University of Skopje.

## Contents

- [T1-unit_testing_and_integration_testing_in_aspnet_core.pdf](T1-unit_testing_and_integration_testing_in_aspnet_core.pdf): Topic 1 slide deck
- [T2-web_api_integration_testing_in_complex_scenarios.pdf](T2-web_api_integration_testing_in_complex_scenarios.pdf): Topic 2 slide deck

## Lecture Summaries

### T1: Unit Testing and Integration Testing in ASP.NET Core

This lecture gives a practical overview of testing strategies in ASP.NET Core applications through a simple MVC example. It starts with data preparation, including realistic sample data generation for both demo and test scenarios, then moves to controller unit testing with mocked dependencies.

It then explains why unit tests alone are not enough and introduces integration testing with ASP.NET Core and EF Core InMemory, followed by cases where a real PostgreSQL test database is still useful. The topic concludes with UI testing through Playwright to validate the application from the user's perspective.

### T2: Web API Integration Testing in Complex Scenarios

This lecture builds on Topic 1 and focuses on integration testing in more demanding Web API scenarios. It covers authentication and authorization testing for OAuth2-protected APIs, including approaches for simulating or mocking tokens without depending on external identity providers.

It also explores testing asynchronous workflows built around a message bus, verifying both publication and consumption of messages. The last part uses BenchmarkDotNet to compare design and implementation choices such as Entity Framework, mediator usage, reflection, and message-bus-related trade-offs.
