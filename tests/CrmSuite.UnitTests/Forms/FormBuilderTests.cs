using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Commands;
using CrmSuite.Modules.Forms.Application.Mapping;
using CrmSuite.Modules.Forms.Application.Schema;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Forms.Infrastructure.Validation;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Forms;

public sealed class FormBuilderTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

    private const string NewSchema = """
        {
          "fields": [
            {
              "key": "bandwidth_mb",
              "label": "پهنای باند",
              "type": "number",
              "required": true,
              "min": 1,
              "max": 10000,
              "order": 1
            },
            {
              "key": "medium",
              "label": "بستر",
              "type": "select",
              "options": [
                { "value": "wireless", "label": "وایرلس" },
                { "value": "fiber", "label": "فیبر" }
              ],
              "order": 2
            },
            {
              "key": "tower_height",
              "label": "ارتفاع دکل",
              "type": "number",
              "required": true,
              "visibleIf": { "field": "medium", "equals": "wireless" },
              "order": 3
            }
          ]
        }
        """;

    [Fact]
    public void Parser_reads_fields_options_and_visibility()
    {
        var result = FormSchemaParser.Parse(NewSchema);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Fields.Should().HaveCount(3);
        result.Value.Fields[1].FieldType.Should().Be(FormFieldType.Select);
        result.Value.Fields[1].OptionsJson.Should().Contain("wireless");
        result.Value.Fields[2].VisibleIfJson.Should().Contain("medium");
    }

    [Fact]
    public void Validator_skips_required_fields_hidden_by_visible_if()
    {
        var validator = new JsonSchemaValidator();

        var hidden = validator.Validate(
            NewSchema,
            """{"bandwidth_mb":100,"medium":"fiber"}""");
        var visible = validator.Validate(
            NewSchema,
            """{"bandwidth_mb":100,"medium":"wireless"}""");

        hidden.Should().BeEmpty();
        visible.Should().Contain(error => error.Contains("ارتفاع دکل"));
    }

    [Fact]
    public void Publish_rebuilds_fields_and_increments_version()
    {
        var template = new FormTemplate(
            "FORM-BW",
            "پهنای باند",
            FormEntityType.Service,
            null,
            null,
            NewSchema,
            1,
            FormDefinitionStatus.Draft,
            null,
            Now);
        var schema = FormSchemaParser.Parse(NewSchema).Value!;

        template.Publish(
            schema.Fields.Select(field => field.ToEntity(template.Id)).ToArray(),
            null,
            Now);

        template.Status.Should().Be(FormDefinitionStatus.Published);
        template.Version.Should().Be(2);
        template.Fields.Should().HaveCount(3);
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Order_alias_is_written_as_entity_and_not_legacy_column()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var template = new FormTemplate(
            "FORM-ORDER",
            "فرم سفارش",
            FormEntityType.Order,
            null,
            null,
            NewSchema,
            1,
            FormDefinitionStatus.Published,
            userId,
            Now);

        FormSubmission? saved = null;
        var templates = new Mock<IFormTemplateRepository>();
        templates.Setup(repository => repository.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var submissions = new Mock<IFormSubmissionRepository>();
        submissions.Setup(repository => repository.Add(It.IsAny<FormSubmission>()))
            .Callback<FormSubmission>(submission => saved = submission);
        var customers = new Mock<ICustomerRepository>();
        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);
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

        var result = await handler.Handle(
            new SubmitFormCommand(
                template.Id,
                null,
                orderId,
                """{"bandwidth_mb":50,"medium":"fiber"}""",
                false,
                FormEntityType.Order,
                orderId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.EntityType.Should().Be(FormEntityType.Order);
        saved.EntityId.Should().Be(orderId);
        saved.CustomerId.Should().BeNull();
        saved.OrderId.Should().BeNull();
        saved.FormVersion.Should().Be(1);
        saved.Items.Should().HaveCount(2);
    }
}
