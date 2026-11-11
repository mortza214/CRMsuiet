using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Commands;
using CrmSuite.Modules.Forms.Application.Mapping;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Infrastructure.Validation;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Forms;

public sealed class FormSubmissionTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Valid_submission_is_persisted()
    {
        var context = CreateContext();
        var result = await context.Handler.Handle(
            new SubmitFormCommand(
                context.Template.Id,
                null,
                null,
                "{\"company\":\"CrmSuite\",\"employees\":25}",
                true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.Submission.Should().NotBeNull();
        context.Submission!.FormTemplateId.Should().Be(context.Template.Id);
    }

    [Fact]
    public async Task Submission_missing_required_field_is_rejected()
    {
        var context = CreateContext();
        var result = await context.Handler.Handle(
            new SubmitFormCommand(
                context.Template.Id,
                null,
                null,
                "{\"employees\":25}",
                true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("Schema");
        context.Submission.Should().BeNull();
    }

    private static FormTestContext CreateContext()
    {
        var userId = Guid.NewGuid();
        var customer = new Customer(
            CustomerType.Natural,
            "مشتری",
            "فرم",
            null,
            null,
            "0012345678",
            "09120000001",
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            userId);

        const string schema = """
            {
              "type": "object",
              "additionalProperties": false,
              "required": ["company", "employees"],
              "properties": {
                "company": { "type": "string", "minLength": 2 },
                "employees": { "type": "integer", "minimum": 1 }
              }
            }
            """;

        var template = new FormTemplate(
            "فرم دیتاسنتر",
            schema,
            1,
            true,
            userId,
            Now);

        var templates = new Mock<IFormTemplateRepository>();
        templates.Setup(repository => repository.GetByIdAsync(
                template.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var submissions = new Mock<IFormSubmissionRepository>();
        FormSubmission? saved = null;
        submissions.Setup(repository => repository.Add(It.IsAny<FormSubmission>()))
            .Callback<FormSubmission>(submission => saved = submission);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByUserIdAsync(
                userId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        customers.Setup(repository => repository.GetByIdAsync(
                customer.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);

        var unitOfWork = new Mock<IFormUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SubmitFormCommandHandler(
            templates.Object,
            submissions.Object,
            new JsonSchemaValidator(),
            unitOfWork.Object,
            customers.Object,
            Mock.Of<IAuditScope>(),
            currentUser.Object,
            clock.Object,
            new FormDtoMapper(new JalaliDateService()));

        return new FormTestContext(handler, template, () => saved);
    }

    private sealed record FormTestContext(
        SubmitFormCommandHandler Handler,
        FormTemplate Template,
        Func<FormSubmission?> SavedAccessor)
    {
        public FormSubmission? Submission => SavedAccessor();
    }
}
