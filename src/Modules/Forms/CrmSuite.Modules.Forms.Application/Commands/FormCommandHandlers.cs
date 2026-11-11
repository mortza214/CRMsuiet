using System.Text.Json;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Dtos;
using CrmSuite.Modules.Forms.Application.Mapping;
using CrmSuite.Modules.Forms.Application.Schema;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Forms.Application.Commands;

public sealed class CreateFormTemplateCommandHandler
    : IRequestHandler<CreateFormTemplateCommand, Result<FormTemplateDto>>
{
    private readonly IFormTemplateRepository _templates;
    private readonly IFormUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly FormDtoMapper _mapper;

    public CreateFormTemplateCommandHandler(
        IFormTemplateRepository templates,
        IFormUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        FormDtoMapper mapper)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<FormTemplateDto>> Handle(
        CreateFormTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var schema = FormSchemaParser.Parse(request.SchemaJson, allowEmpty: !request.IsActive);
        if (schema.IsFailure)
        {
            return Result.Failure<FormTemplateDto>(schema.Errors, schema.Message);
        }

        var code = string.IsNullOrWhiteSpace(request.Code)
            ? $"FRM-{Guid.NewGuid():N}"[..16].ToUpperInvariant()
            : request.Code.Trim().ToUpperInvariant();

        if (await _templates.CodeExistsAsync(code, null, cancellationToken))
        {
            return Result.Failure<FormTemplateDto>(
                "کد فرم قبلاً استفاده شده است.",
                "FORMS_CODE_DUPLICATE");
        }

        var status = request.IsActive
            ? FormDefinitionStatus.Published
            : FormDefinitionStatus.Draft;
        var template = new FormTemplate(
            code,
            request.Title,
            request.EntityType,
            request.TargetServiceId,
            request.TargetServiceCategoryId,
            request.SchemaJson,
            request.Version,
            status,
            _currentUser.UserId,
            _clock.UtcNow);

        if (status == FormDefinitionStatus.Published)
        {
            foreach (var field in schema.Value!.Fields)
            {
                template.Fields.Add(field.ToEntity(template.Id));
            }
        }

        _templates.Add(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = template.Id.ToString();

        return Result.Success(_mapper.ToTemplateDto(template));
    }
}

public sealed class UpdateFormTemplateCommandHandler
    : IRequestHandler<UpdateFormTemplateCommand, Result<FormTemplateDto>>
{
    private readonly IFormTemplateRepository _templates;
    private readonly IFormUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly FormDtoMapper _mapper;

    public UpdateFormTemplateCommandHandler(
        IFormTemplateRepository templates,
        IFormUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        FormDtoMapper mapper)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<FormTemplateDto>> Handle(
        UpdateFormTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetByIdWithFieldsAsync(request.Id, cancellationToken);
        if (template is null)
        {
            return Result.Failure<FormTemplateDto>("فرم یافت نشد.", "FORMS_TEMPLATE_NOT_FOUND");
        }

        var schema = FormSchemaParser.Parse(request.SchemaJson, allowEmpty: true);
        if (schema.IsFailure)
        {
            return Result.Failure<FormTemplateDto>(schema.Errors, schema.Message);
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _templates.CodeExistsAsync(code, template.Id, cancellationToken))
        {
            return Result.Failure<FormTemplateDto>(
                "کد فرم قبلاً استفاده شده است.",
                "FORMS_CODE_DUPLICATE");
        }

        try
        {
            template.UpdateDraft(
                code,
                request.Title,
                request.EntityType,
                request.TargetServiceId,
                request.TargetServiceCategoryId,
                request.SchemaJson,
                _currentUser.UserId,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = template.Id.ToString();
            return Result.Success(_mapper.ToTemplateDto(template));
        }
        catch (DomainException exception)
        {
            return Result.Failure<FormTemplateDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class PublishFormTemplateCommandHandler
    : IRequestHandler<PublishFormTemplateCommand, Result<FormTemplateDto>>
{
    private readonly IFormTemplateRepository _templates;
    private readonly IFormUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly FormDtoMapper _mapper;

    public PublishFormTemplateCommandHandler(
        IFormTemplateRepository templates,
        IFormUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        FormDtoMapper mapper)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<FormTemplateDto>> Handle(
        PublishFormTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetByIdWithFieldsAsync(request.Id, cancellationToken);
        if (template is null)
        {
            return Result.Failure<FormTemplateDto>("فرم یافت نشد.", "FORMS_TEMPLATE_NOT_FOUND");
        }

        var schema = FormSchemaParser.Parse(template.SchemaJson);
        if (schema.IsFailure)
        {
            return Result.Failure<FormTemplateDto>(schema.Errors, schema.Message);
        }

        try
        {
            template.Publish(
                schema.Value!.Fields.Select(field => field.ToEntity(template.Id)).ToArray(),
                _currentUser.UserId,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = template.Id.ToString();
            return Result.Success(_mapper.ToTemplateDto(template));
        }
        catch (DomainException exception)
        {
            return Result.Failure<FormTemplateDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class ArchiveFormTemplateCommandHandler
    : IRequestHandler<ArchiveFormTemplateCommand, Result<FormTemplateDto>>
{
    private readonly IFormTemplateRepository _templates;
    private readonly IFormUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly FormDtoMapper _mapper;

    public ArchiveFormTemplateCommandHandler(
        IFormTemplateRepository templates,
        IFormUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        FormDtoMapper mapper)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<FormTemplateDto>> Handle(
        ArchiveFormTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetByIdWithFieldsAsync(request.Id, cancellationToken);
        if (template is null)
        {
            return Result.Failure<FormTemplateDto>("فرم یافت نشد.", "FORMS_TEMPLATE_NOT_FOUND");
        }

        try
        {
            template.Archive(_currentUser.UserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = template.Id.ToString();
            return Result.Success(_mapper.ToTemplateDto(template));
        }
        catch (DomainException exception)
        {
            return Result.Failure<FormTemplateDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class SubmitFormCommandHandler
    : IRequestHandler<SubmitFormCommand, Result<FormSubmissionDto>>
{
    private readonly IFormTemplateRepository _templates;
    private readonly IFormSubmissionRepository _submissions;
    private readonly IFormSchemaValidator _schemaValidator;
    private readonly IFormUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly FormDtoMapper _mapper;

    public SubmitFormCommandHandler(
        IFormTemplateRepository templates,
        IFormSubmissionRepository submissions,
        IFormSchemaValidator schemaValidator,
        IFormUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        FormDtoMapper mapper)
    {
        _templates = templates;
        _submissions = submissions;
        _schemaValidator = schemaValidator;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<FormSubmissionDto>> Handle(
        SubmitFormCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<FormSubmissionDto>(
                "کاربر جاری قابل تشخیص نیست.",
                "FORMS_CURRENT_USER_MISSING");
        }

        var template = await _templates.GetByIdAsync(request.FormTemplateId, cancellationToken);
        if (template is null)
        {
            return Result.Failure<FormSubmissionDto>("فرم یافت نشد.", "FORMS_TEMPLATE_NOT_FOUND");
        }

        if (template.Status != FormDefinitionStatus.Published)
        {
            return Result.Failure<FormSubmissionDto>("فرم فعال نیست.", "FORMS_TEMPLATE_INACTIVE");
        }

        var entity = await ResolveEntityAsync(request, cancellationToken);
        if (entity.IsFailure)
        {
            return Result.Failure<FormSubmissionDto>(entity.Errors, entity.Message);
        }

        if (entity.Value!.EntityType == FormEntityType.Customer
            && entity.Value.EntityId.HasValue
            && await _customers.GetByIdAsync(
                entity.Value.EntityId.Value,
                false,
                cancellationToken) is null)
        {
            return Result.Failure<FormSubmissionDto>(
                "مشتری یافت نشد.",
                "FORMS_CUSTOMER_NOT_FOUND");
        }

        var errors = _schemaValidator.Validate(template.SchemaJson, request.SubmittedDataJson);
        if (errors.Count > 0)
        {
            return Result.Failure<FormSubmissionDto>(
                errors,
                "داده‌های فرم با Schema مطابقت ندارد.");
        }

        var submission = new FormSubmission(
            template.Id,
            template.Version,
            entity.Value!.EntityType,
            entity.Value.EntityId,
            request.SubmittedDataJson,
            _currentUser.UserId.Value,
            _clock.UtcNow);

        AddSubmissionItems(submission, template, request.SubmittedDataJson);

        _submissions.Add(submission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = submission.Id.ToString();

        var customerName = entity.Value.EntityType == FormEntityType.Customer
            && entity.Value.EntityId.HasValue
            ? (await _customers.GetByIdAsync(
                entity.Value.EntityId.Value,
                false,
                cancellationToken))?.DisplayName
            : null;

        return Result.Success(
            _mapper.ToSubmissionDto(submission, template.Title, customerName));
    }

    private async Task<Result<ResolvedEntity>> ResolveEntityAsync(
        SubmitFormCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Portal)
        {
            if (_currentUser.UserId is null)
            {
                return Result.Failure<ResolvedEntity>(
                    "کاربر جاری قابل تشخیص نیست.",
                    "FORMS_CURRENT_USER_MISSING");
            }

            var customer = await _customers.GetByUserIdAsync(
                _currentUser.UserId.Value,
                false,
                cancellationToken);
            return customer is null
                ? Result.Failure<ResolvedEntity>("پروفایل مشتری یافت نشد.", "FORMS_CUSTOMER_NOT_FOUND")
                : Result.Success(new ResolvedEntity(FormEntityType.Customer, customer.Id));
        }

        if (request.EntityType.HasValue && request.EntityType.Value != FormEntityType.None)
        {
            return request.EntityId.HasValue
                ? Result.Success(new ResolvedEntity(request.EntityType.Value, request.EntityId))
                : Result.Failure<ResolvedEntity>(
                    "شناسه موجودیت الزامی است.",
                    "FORMS_ENTITY_ID_REQUIRED");
        }

        if (request.CustomerId.HasValue)
        {
            var customer = await _customers.GetByIdAsync(
                request.CustomerId.Value,
                false,
                cancellationToken);
            return customer is null
                ? Result.Failure<ResolvedEntity>("مشتری یافت نشد.", "FORMS_CUSTOMER_NOT_FOUND")
                : Result.Success(new ResolvedEntity(FormEntityType.Customer, customer.Id));
        }

        if (request.OrderId.HasValue)
        {
            return Result.Success(new ResolvedEntity(FormEntityType.Order, request.OrderId));
        }

        return Result.Failure<ResolvedEntity>(
            "انتخاب مشتری یا موجودیت الزامی است.",
            "FORMS_CUSTOMER_REQUIRED");
    }

    private static void AddSubmissionItems(
        FormSubmission submission,
        FormTemplate template,
        string submittedDataJson)
    {
        var schema = FormSchemaParser.Parse(template.SchemaJson);
        if (schema.IsFailure)
        {
            return;
        }

        using var document = JsonDocument.Parse(submittedDataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var field in schema.Value!.Fields)
        {
            if (!document.RootElement.TryGetProperty(field.Key, out var value)
                || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            decimal? number = null;
            string? text;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric))
            {
                number = numeric;
                text = numeric.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (value.ValueKind == JsonValueKind.String)
            {
                text = value.GetString();
            }
            else
            {
                text = value.GetRawText();
            }

            submission.AddItem(new FormSubmissionItem(
                submission.Id,
                field.Key,
                field.FieldType,
                text,
                number));
        }
    }

    private sealed record ResolvedEntity(FormEntityType EntityType, Guid? EntityId);
}

public sealed class ReviewFormSubmissionCommandHandler
    : IRequestHandler<ReviewFormSubmissionCommand, Result<FormSubmissionDto>>
{
    private readonly IFormSubmissionRepository _submissions;
    private readonly IFormTemplateRepository _templates;
    private readonly ICustomerRepository _customers;
    private readonly IFormUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly FormDtoMapper _mapper;

    public ReviewFormSubmissionCommandHandler(
        IFormSubmissionRepository submissions,
        IFormTemplateRepository templates,
        ICustomerRepository customers,
        IFormUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        FormDtoMapper mapper)
    {
        _submissions = submissions;
        _templates = templates;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<FormSubmissionDto>> Handle(
        ReviewFormSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<FormSubmissionDto>(
                "کاربر جاری قابل تشخیص نیست.",
                "FORMS_CURRENT_USER_MISSING");
        }

        var submission = await _submissions.GetByIdAsync(request.SubmissionId, cancellationToken);
        if (submission is null)
        {
            return Result.Failure<FormSubmissionDto>("پاسخ فرم یافت نشد.", "FORMS_SUBMISSION_NOT_FOUND");
        }

        try
        {
            if (request.Approve)
            {
                submission.Approve(_currentUser.UserId.Value, _clock.UtcNow, request.Note);
            }
            else
            {
                submission.Reject(_currentUser.UserId.Value, _clock.UtcNow, request.Note);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = submission.Id.ToString();

            var template = await _templates.GetByIdAsync(
                submission.FormTemplateId,
                cancellationToken);
            var customerName = submission.EntityType == FormEntityType.Customer
                && submission.EntityId.HasValue
                ? (await _customers.GetByIdAsync(
                    submission.EntityId.Value,
                    false,
                    cancellationToken))?.DisplayName
                : null;

            return Result.Success(
                _mapper.ToSubmissionDto(submission, template?.Title, customerName));
        }
        catch (DomainException exception)
        {
            return Result.Failure<FormSubmissionDto>(exception.Message, exception.Code);
        }
    }
}
