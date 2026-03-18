namespace MegaPricer.Dtos;

public record CabinetDto
{
    public int cabinetId;
    public float thisPartWidth;
    public float thisPartDepth;
    public float thisPartHeight;
    public int thisPartColorId;
    public string thisPartSku;
    public decimal thisPartCost = 0;
    public int thisSectionWidth = 0;
}
