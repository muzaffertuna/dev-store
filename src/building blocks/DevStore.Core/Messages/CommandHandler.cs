using DevStore.Core.Data;
using FluentValidation.Results;
using System.Threading.Tasks;

namespace DevStore.Core.Messages
{
    public abstract class CommandHandler
    {
        protected ValidationResult ValidationResult;

        protected CommandHandler()
        {
            ValidationResult = new ValidationResult();
        }

        protected void AdicionarErro(string mensagem)
        {
            ValidationResult.Errors.Add(new ValidationFailure(string.Empty, mensagem));
        }

        protected async Task<ValidationResult> PersistData(IUnitOfWork uow)
        {
            if (!await uow.Commit()) AdicionarErro("An error occurred while trying to persist data");

            return ValidationResult;
        }
    }
}