using ChocolateToxicity.Clinical;

namespace ChocolateToxicity.Clinical.Tests;

public class DrugDoseTests
{
    [Fact]
    public void Dose_gives_mg_ml_and_tablets()
    {
        var result = DrugDose.Calculate(weightKg: 20, doseMgPerKg: 2, concentrationMgPerMl: 10, tabletStrengthMg: 25);

        Assert.Equal(40, result.TotalMg, 6);
        Assert.Equal(4, result.VolumeMl!.Value, 6);
        Assert.Equal(1.6, result.Tablets!.Value, 6);
    }

    [Fact]
    public void Volume_and_tablets_are_left_out_without_a_strength()
    {
        var result = DrugDose.Calculate(10, 1);

        Assert.Null(result.VolumeMl);
        Assert.Null(result.Tablets);
    }

    [Theory]
    [InlineData(1.6, 2, 1.5)]
    [InlineData(1.6, 4, 1.5)]
    [InlineData(1.65, 4, 1.75)]
    [InlineData(0.2, 1, 0)]
    public void Tablets_round_to_the_nearest_split(double tablets, int split, double expected)
    {
        Assert.Equal(expected, DrugDose.RoundToSplit(tablets, split), 6);
    }

    [Fact]
    public void Weight_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DrugDose.Calculate(0, 1));
    }
}

public class FluidTherapyTests
{
    [Theory]
    [InlineData(Species.Dog, 10, 742.3)] // 132 x 10^0.75 = 132 x 5.623
    [InlineData(Species.Cat, 4, 226.3)] // 80 x 4^0.75 = 80 x 2.828
    public void Maintenance_follows_the_AAHA_formulas(Species species, double weightKg, double expected)
    {
        Assert.Equal(expected, FluidTherapy.MaintenanceMlPerDay(species, weightKg), 1);
    }

    [Fact]
    public void Deficit_is_percent_times_weight_in_litres()
    {
        Assert.Equal(1600, FluidTherapy.DeficitMl(8, 20), 6);
    }

    [Fact]
    public void Plan_adds_maintenance_deficit_and_losses()
    {
        var plan = FluidTherapy.Plan(Species.Dog, 20, dehydrationPercent: 6, replaceDeficitOverHours: 12, ongoingLossesMlPerDay: 240);

        Assert.Equal(100, plan.DeficitMlPerHour, 6);
        Assert.Equal(10, plan.OngoingLossesMlPerHour, 6);
        Assert.Equal(132 * Math.Pow(20, 0.75) / 24, plan.MaintenanceMlPerHour, 6);
        Assert.Equal(plan.MaintenanceMlPerHour + 110, plan.TotalMlPerHour, 6);
        Assert.Equal(plan.MaintenanceMlPerHour + 10, plan.AfterDeficitMlPerHour, 6);
    }

    [Fact]
    public void Drip_rate_uses_the_giving_set_factor()
    {
        Assert.Equal(20, FluidTherapy.DropsPerMinute(60, 20), 6);
        Assert.Equal(60, FluidTherapy.DropsPerMinute(60, 60), 6);
    }

    [Fact]
    public void Implausible_dehydration_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FluidTherapy.DeficitMl(20, 10));
    }
}

public class ConstantRateInfusionTests
{
    [Theory]
    [InlineData(10, CriDoseUnit.MicrogramsPerKgPerMinute, 0.6)]
    [InlineData(600, CriDoseUnit.MicrogramsPerKgPerHour, 0.6)]
    [InlineData(0.6, CriDoseUnit.MilligramsPerKgPerHour, 0.6)]
    [InlineData(0.01, CriDoseUnit.MilligramsPerKgPerMinute, 0.6)]
    public void Units_convert_to_mg_per_kg_per_hour(double dose, CriDoseUnit unit, double expected)
    {
        Assert.Equal(expected, ConstantRateInfusion.ToMgPerKgPerHour(dose, unit), 9);
    }

    [Fact]
    public void Lidocaine_in_a_bag()
    {
        // 20 kg dog, lidocaine 50 µg/kg/min (3 mg/kg/hr = 60 mg/hr), 2% (20 mg/mL), 500 mL bag at 50 mL/hr:
        // the bag lasts 10 hours, so it needs 600 mg = 30 mL.
        var (mg, ml) = ConstantRateInfusion.AddToBag(20, 50, CriDoseUnit.MicrogramsPerKgPerMinute, 20, 500, 50);

        Assert.Equal(600, mg, 6);
        Assert.Equal(30, ml, 6);
    }

    [Fact]
    public void Undiluted_rate_for_a_syringe_driver()
    {
        // 10 kg, fentanyl 5 µg/kg/hr = 0.05 mg/hr; 0.05 mg/mL stock gives 1 mL/hr.
        Assert.Equal(1, ConstantRateInfusion.UndilutedMlPerHour(10, 5, CriDoseUnit.MicrogramsPerKgPerHour, 0.05), 6);
    }
}

public class BodySurfaceAreaTests
{
    [Theory]
    [InlineData(Species.Dog, 10, 0.469)] // Merck dog table
    [InlineData(Species.Dog, 20, 0.744)]
    [InlineData(Species.Cat, 4, 0.252)]
    public void Matches_the_Merck_tables(Species species, double weightKg, double expected)
    {
        Assert.Equal(expected, BodySurfaceArea.SquareMetres(species, weightKg), 3);
    }
}

public class EnergyRequirementTests
{
    [Fact]
    public void Rer_is_70_times_weight_to_the_three_quarters()
    {
        Assert.Equal(393.6, EnergyRequirement.RestingKcalPerDay(10), 1);
    }

    [Fact]
    public void Every_species_has_factors()
    {
        Assert.Contains(EnergyRequirement.Factors, f => f.Species == Species.Dog);
        Assert.Contains(EnergyRequirement.Factors, f => f.Species == Species.Cat);
    }
}

public class TransfusionTests
{
    [Fact]
    public void Volume_follows_the_Merck_formula()
    {
        // 20 kg dog from PCV 15% to 25% with 40% pRBC: 10/40 x 90 x 20 = 450 mL.
        Assert.Equal(450, Transfusion.VolumeMl(20, 15, 25, 40, Transfusion.DefaultBloodVolumeMlPerKg(Species.Dog)), 6);
    }

    [Fact]
    public void Cats_use_a_smaller_blood_volume()
    {
        Assert.Equal(60, Transfusion.DefaultBloodVolumeMlPerKg(Species.Cat));
    }

    [Theory]
    [InlineData(25, 20)]
    [InlineData(15, 45)]
    public void Target_must_sit_between_patient_and_donor(double recipient, double target)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Transfusion.VolumeMl(10, recipient, target, 40, 90));
    }
}

public class EmeticsTests
{
    [Theory]
    [InlineData(1.8, 1)]
    [InlineData(5, 1)]
    [InlineData(5.1, 2)]
    [InlineData(10, 2)]
    [InlineData(10.1, 3)]
    [InlineData(20, 3)]
    [InlineData(20.1, 4)]
    [InlineData(35, 4)]
    [InlineData(35.1, 6)]
    [InlineData(60, 6)]
    [InlineData(60.1, 8)]
    [InlineData(100, 8)]
    public void Clevor_drops_follow_the_SPC_table(double weightKg, int drops)
    {
        var dose = Emetics.Ropinirole(weightKg)!;

        Assert.Equal(drops, dose.Drops);
        Assert.Equal(drops * 810, dose.Micrograms, 6);
    }

    [Theory]
    [InlineData(1.7)]
    [InlineData(100.5)]
    public void Clevor_has_no_dose_outside_its_weight_range(double weightKg)
    {
        Assert.Null(Emetics.Ropinirole(weightKg));
    }

    [Fact]
    public void Six_drops_are_given_in_two_goes()
    {
        Assert.Contains("1-2 minutes later", Emetics.Ropinirole(40)!.Instructions);
    }
}

public class EmergencyDrugsTests
{
    [Fact]
    public void Adrenaline_low_dose_for_a_10_kg_dog()
    {
        var adrenaline = EmergencyDrugs.All.First(d => d.Name.StartsWith("Adrenaline"));

        Assert.Equal(0.1, adrenaline.Dose(10), 6);
        Assert.Equal(0.1, adrenaline.VolumeMl(10, adrenaline.ConcentrationPerMl), 6);
    }

    [Fact]
    public void Defibrillation_energy_by_waveform()
    {
        Assert.Equal((40, 60), EmergencyDrugs.ExternalDefibrillation(10, biphasic: false));
        Assert.Equal((20, 40), EmergencyDrugs.ExternalDefibrillation(10, biphasic: true));
        Assert.Equal((5, 10), EmergencyDrugs.InternalDefibrillation(10, biphasic: false));
    }
}

public class UnitConversionTests
{
    [Fact]
    public void Conversions_round_trip()
    {
        Assert.Equal(22.046, UnitConversion.KgToLb(10), 3);
        Assert.Equal(10, UnitConversion.LbToKg(UnitConversion.KgToLb(10)), 9);
        Assert.Equal(101.5, UnitConversion.CelsiusToFahrenheit(38.6111111), 3);
        Assert.Equal(38.5, UnitConversion.FahrenheitToCelsius(101.3), 3);
        Assert.Equal(20, UnitConversion.PercentToMgPerMl(2), 6);
        Assert.Equal(2, UnitConversion.MgPerMlToPercent(20), 6);
    }
}
