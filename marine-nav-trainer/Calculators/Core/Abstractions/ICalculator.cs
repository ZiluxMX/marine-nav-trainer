namespace marine_nav_trainer.Calculators.Core.Abstractions {
    public interface ICalculator<TInput, TResult> {
        TResult Calculate(TInput input);
    }
}
