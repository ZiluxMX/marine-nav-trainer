using marine_nav_trainer.Calculators.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace marine_nav_trainer.Calculators.Core.Factory {
    public class CalculatorFactory {
        private readonly Dictionary<CalculatorType, object> _calculators = new();

        public void Register<TInput, TResult>(
            CalculatorType type,
            ICalculator<TInput, TResult> calculator) {
            _calculators[type] = calculator;
        }

        public ICalculator<TInput, TResult> Get<TInput, TResult>(CalculatorType type) {
            if (!_calculators.TryGetValue(type, out var calc))
                throw new InvalidOperationException($"Calculator '{type}' not registered!");
            return (ICalculator<TInput, TResult>)calc;
        }
    }
}