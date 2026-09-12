using FluentValidation;
using Hub.Application.Features.Common.Contracts;
using Hub.Application.Features.Events.Commands.Create;
using Hub.Application.Features.Events.Commands.CreateParticipant;
using Hub.Application.Features.Events.Commands.CreateParticipantRole;
using Hub.Application.Features.Events.Commands.CreateRequirement;
using Hub.Application.Features.Events.Commands.CreateRequirementVerifier;
using Hub.Application.Features.Events.Commands.CreateRequirementVerifier.Appliers;
using Hub.Application.Features.Events.Commands.CreateRole;
using Hub.Application.Features.Events.Commands.DeleteDescription;
using Hub.Application.Features.Events.Commands.DeleteLocation;
using Hub.Application.Features.Events.Commands.DeleteParticipantRole;
using Hub.Application.Features.Events.Commands.DeleteRequirement;
using Hub.Application.Features.Events.Commands.DeleteRequirementVerifier;
using Hub.Application.Features.Events.Commands.DeleteRole;
using Hub.Application.Features.Events.Commands.UpdateCompletion;
using Hub.Application.Features.Events.Commands.UpdateDates;
using Hub.Application.Features.Events.Commands.UpdateDescription;
using Hub.Application.Features.Events.Commands.UpdateLocation;
using Hub.Application.Features.Events.Commands.ReconcileState;
using Hub.Application.Features.Events.Commands.UpdateRequirement;
using Hub.Application.Features.Events.Commands.UpdateState;
using Hub.Application.Features.Events.Commands.UpdateTitle;
using Hub.Application.Features.Events.Contracts;
using Hub.Application.Features.Events.Queries.Get;
using Hub.Application.Features.Events.Queries.GetById;
using Hub.Application.Features.Events.Queries.GetParticipantById;
using Hub.Application.Features.Events.Queries.GetRequirementById;
using Hub.Application.Features.Groups.Commands.AttachEvent;
using Hub.Application.Features.Groups.Commands.Create;
using Hub.Application.Features.Groups.Contracts;
using Hub.Application.Features.Groups.Queries.Get;
using Hub.Application.Features.Groups.Queries.GetEvents;
using Hub.Application.Features.Payments.Commands.ConfirmDonation;
using Hub.Application.Features.Payments.Commands.CreateDonation;
using Hub.Application.Features.Payments.Commands.HandlePaymentFailed;
using Hub.Application.Features.Payments.Commands.HandlePaymentSucceeded;
using Hub.Application.Features.Payments.Commands.HandleRefundSucceeded;
using Hub.Application.Features.Payments.Commands.ProcessPaymentWebhook;
using Hub.Application.Features.Payments.Commands.ReceivePaymentWebhook;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Features.Payments.Queries.GetDonationById;
using Hub.Application.Features.Users.Contracts;
using Hub.Application.Features.Users.Queries.Get;
using Hub.Application.Features.Users.Queries.GetById;
using Hub.Application.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Hub.Application;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddApplication()
        {
            services.AddValidatorsFromAssemblyContaining<CreateEventRequest>(
                includeInternalTypes: true);
        
            services.AddUserHandlers();
            services.AddEventHandlers();
            services.AddGroupHandlers();
            services.AddPaymentHandlers();
        }

        void AddUserHandlers()
        {
            services.AddDecoratedHandler<GetUserQuery, ListResponse<UserSummaryResponse>, GetUserQueryHandler>();
            services.AddDecoratedHandler<GetUserByIdQuery, UserDetailsResponse, GetUserByIdQueryHandler>();
        }

        void AddEventHandlers()
        {
            services.AddDecoratedHandler<CreateEventCommand, EventSummaryResponse, CreateEventCommandHandler>();
            services.AddDecoratedHandler<GetEventQuery, ListResponse<EventSummaryResponse>, GetEventQueryHandler>();
            services.AddDecoratedHandler<GetEventByIdQuery, EventDetailsResponse, GetEventByIdQueryHandler>();
            services.AddDecoratedHandler<UpdateTitleCommand, IdResponse, UpdateTitleCommandHandler>();
            services.AddDecoratedHandler<UpdateDescriptionCommand, IdResponse, UpdateDescriptionCommandHandler>();
            services.AddDecoratedHandler<DeleteDescriptionCommand, IdResponse, DeleteDescriptionCommandHandler>();
            services.AddDecoratedHandler<UpdateDatesCommand, IdResponse, UpdateDatesCommandHandler>();
            services.AddDecoratedHandler<UpdateLocationCommand, IdResponse, UpdateLocationCommandHandler>();
            services.AddDecoratedHandler<DeleteLocationCommand, IdResponse, DeleteLocationCommandHandler>();
            services.AddDecoratedHandler<CreateParticipantCommand, EventParticipantSummaryResponse, CreateParticipantCommandHandler>();
            services.AddDecoratedHandler<CreateParticipantRoleCommand, EventParticipantRoleResponse, CreateParticipantRoleCommandHandler>();
            services.AddDecoratedHandler<CreateRoleCommand, EventRoleSummaryResponse, CreateRoleCommandHandler>();
            services.AddDecoratedHandler<CreateRequirementCommand, EventRequirementSummaryResponse, CreateRequirementCommandHandler>();
            services.AddDecoratedHandler<GetParticipantByIdQuery, EventParticipantDetailsResponse, GetParticipantByIdQueryHandler>();
            services.AddDecoratedHandler<GetRequirementQuery, EventRequirementDetailsResponse, GetRequirementByIdQueryHandler>();
            services.AddDecoratedHandler<UpdateStateCommand, IdResponse, UpdateStateCommandHandler>();
            services.AddDecoratedHandler<ReconcileStateCommand, IdResponse, ReconcileStateCommandHandler>();
            services.AddDecoratedHandler<UpdateRequirementCommand, IdResponse, UpdateRequirementCommandHandler>();
            services.AddDecoratedHandler<DeleteRequirementCommand, IdResponse, DeleteRequirementCommandHandler>();
            services.AddDecoratedHandler<CreateRequirementVerifierCommand, EventRequirementVerifierSummaryResponse, CreateRequirementVerifierCommandHandler>();
            services.AddDecoratedHandler<DeleteRequirementVerifierCommand, IdResponse, DeleteRequirementVerifierCommandHandler>();
            services.AddDecoratedHandler<DeleteRoleCommand, IdResponse, DeleteRoleCommandHandler>();
            services.AddDecoratedHandler<DeleteParticipantRoleCommand, IdResponse, DeleteParticipantRoleCommandHandler>();
            services.AddDecoratedHandler<UpdateCompletionCommand, IdResponse, UpdateCompletionCommandHandler>();
            
            services.AddScoped<IRequirementVerifierApplier, RequirementRoleVerifierApplier>();
            services.AddScoped<IRequirementVerifierApplier, RequirementParticipantVerifierApplier>();
            services.AddScoped<IRequirementVerifierApplier, RequirementRuleVerifierApplier>();
        }

        void AddGroupHandlers()
        {
            services.AddDecoratedHandler<CreateGroupCommand, GroupSummaryResponse, CreateGroupCommandHandler>();
            services.AddDecoratedHandler<GetGroupQuery, ListResponse<GroupSummaryResponse>, GetGroupQueryHandler>();
            services.AddDecoratedHandler<GetGroupEventsQuery, ListResponse<EventSummaryResponse>, GetGroupEventsQueryHandler>();
            services.AddDecoratedHandler<AttachEventCommand, IdResponse, AttachEventCommandHandler>();
        }

        void AddPaymentHandlers()
        {
            services.AddDecoratedHandler<CreateDonationCommand, DonationResponse, CreateDonationCommandHandler>();
            services.AddDecoratedHandler<ConfirmDonationCommand, DonationResponse, ConfirmDonationCommandHandler>();
            services.AddDecoratedHandler<GetDonationByIdQuery, DonationResponse, GetDonationByIdQueryHandler>();
            services.AddDecoratedHandler<ReceivePaymentWebhookCommand, IdResponse, ReceivePaymentWebhookCommandHandler>();
            services.AddDecoratedHandler<ProcessPaymentWebhookCommand, IdResponse, ProcessPaymentWebhookCommandHandler>();
            services.AddDecoratedHandler<HandlePaymentSucceededCommand, IdResponse, HandlePaymentSucceededCommandHandler>();
            services.AddDecoratedHandler<HandlePaymentFailedCommand, IdResponse, HandlePaymentFailedCommandHandler>();
            services.AddDecoratedHandler<HandleRefundSucceededCommand, IdResponse, HandleRefundSucceededCommandHandler>();
        }
    }
}