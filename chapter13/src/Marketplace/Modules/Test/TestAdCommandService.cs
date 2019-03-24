using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Marketplace.Ads.Domain.ClassifiedAds;
using Marketplace.Ads.Domain.Shared;
using Marketplace.Ads.Domain.Test;
using Marketplace.Ads.Messages.Ads;
using Marketplace.Infrastructure.EventStore;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Modules.Test
{
    public class TestAdCommandService : TestCommandService<TestAdState>
    {
        public TestAdCommandService(TestStore store) : base(store) { }

        public Task Handle(Commands.V1.Create command)
            => Handle(
                command.Id,
                state => TestAd.Create(
                    new ClassifiedAdId(command.Id),
                    new UserId(command.OwnerId)
                )
            );

        public Task Handle(Commands.V1.ChangeTitle command)
            => Handle(
                command.Id,
                state => TestAd.SetTitle(
                    state,
                    ClassifiedAdTitle.FromString(command.Title)
                )
            );
    }

    public abstract class TestCommandService<T>
        where T : IAggregateState<T>, new()
    {
        readonly TestStore _store;

        protected TestCommandService(TestStore store) => _store = store;

        Task<T> Load(Guid id)
            => _store.Load<T>(
                id,
                (x, e) => x.When(e)
            );

        protected async Task Handle(
            Guid id,
            Func<T, (T, IEnumerable<object>)> update)
        {
            var state = await Load(id);
            await _store.Save(state.Version, update(state));
        }
    }

    public static class ControllerExtensions
    {
        public static async Task<ActionResult> HandleCommand<TCommand>(
            this ControllerBase _,
            TCommand command,
            Func<TCommand, Task> handler,
            Action<TCommand> commandModifier = null)
        {
            try
            {
                commandModifier?.Invoke(command);
                await handler(command);
                return new OkResult();
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(
                    new
                    {
                        error = e.Message, 
                        stackTrace = e.StackTrace
                    }
                );
            }
            
        }
    }
}