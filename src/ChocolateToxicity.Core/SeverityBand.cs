namespace ChocolateToxicity.Core;

/// <summary>Expected clinical signs for a methylxanthine dose, in increasing order of severity.</summary>
public enum SeverityBand
{
    /// <summary>Below 20 mg/kg: methylxanthine toxicosis not expected, though fat and sugar can still upset the stomach.</summary>
    None,

    /// <summary>20 mg/kg and above: vomiting, diarrhoea, polydipsia.</summary>
    Mild,

    /// <summary>40 mg/kg and above: cardiotoxic effects such as tachycardia and arrhythmias.</summary>
    Severe,

    /// <summary>60 mg/kg and above: tremors and seizures.</summary>
    Seizures,
}
