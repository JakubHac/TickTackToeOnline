using System;

[Serializable]
public class ClientWelcomeMessageData
{
    public readonly string ClientName;
    public readonly int GemIndex;
    
    public ClientWelcomeMessageData(string clientName, int gemIndex)
    {
        ClientName = clientName;
        GemIndex = gemIndex;
    }
}
