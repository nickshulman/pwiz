using System;
using JetBrains.Annotations;
using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Model
{
    public class FormulaMass : Immutable
    {
        public FormulaMass(string formula) : this(formula, null, null)
        {
            if (string.IsNullOrEmpty(formula))
            {
                throw new ArgumentException();
            }
        }

        public FormulaMass(double averageMass, double monoisotopicMass) : this(null, averageMass, monoisotopicMass)
        {
        }

        [CanBeNull]
        public static FormulaMass FromFormulaOrMass(string formula, double? monoMass, double? averageMass)
        {
            if (!string.IsNullOrEmpty(formula))
            {
                return new FormulaMass(formula);
            }

            if (monoMass.HasValue || averageMass.HasValue)
            {
                return new FormulaMass(monoMass ?? averageMass.Value, averageMass ?? monoMass.Value);
            }

            return null;
        }

        private FormulaMass(string formula, double? averageMass, double? monoMass)
        {
        }

        [CanBeNull]
        public string Formula { get; private set; }

        public double? AverageMass { get; private set; }
        public double? MonoMass { get; private set; }

        protected bool Equals(FormulaMass other)
        {
            return Formula == other.Formula && 
                   Nullable.Equals(AverageMass, other.AverageMass) &&
                   Nullable.Equals(MonoMass, other.MonoMass);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FormulaMass) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Formula != null ? Formula.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ AverageMass.GetHashCode();
                hashCode = (hashCode * 397) ^ MonoMass.GetHashCode();
                return hashCode;
            }
        }
    }
}
