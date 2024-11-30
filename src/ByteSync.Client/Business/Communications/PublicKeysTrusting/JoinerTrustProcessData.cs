using System.Collections.ObjectModel;
using System.Threading;
using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Business.Communications.PublicKeysTrusting;

public class JoinerTrustProcessData
{
    public JoinerTrustProcessData()
    {
        ReceivedMembersPublicKeyCheckDatas = Array.Empty<PublicKeyCheckData?>();
        WaitForAllPublicKeyCheckDatasReceived = new ManualResetEvent(false);
        
        NonStoredPublicKeyCheckDatas = new List<PublicKeyCheckData>();
        FullyTrustedPublicKeyCheckDatas = new List<PublicKeyCheckData>();
    }

    private List<string>? ExpectedPublicKeyCheckDataMembers { get; set; }
    
    public ManualResetEvent WaitForAllPublicKeyCheckDatasReceived { get; }
    
    public List<PublicKeyCheckData> NonStoredPublicKeyCheckDatas { get; }
    
    public List<PublicKeyCheckData> FullyTrustedPublicKeyCheckDatas { get; }

    private PublicKeyCheckData?[] ReceivedMembersPublicKeyCheckDatas { get; set; }

    public void StoreMemberPublicKeyCheckData(PublicKeyCheckData publicKeyCheckData)
    {
        // On travail en temps réel, et ExpectedPublicKeyCheckDataMembers peut ne pas encore avoir été setté
        // Si c'est le cas, on Store dans une liste tampon "NonStoredPublicKeyCheckDatas" qui sera traitée ultérieurement
        if (ExpectedPublicKeyCheckDataMembers == null)
        {
            NonStoredPublicKeyCheckDatas.Add(publicKeyCheckData);
            return;
        }

        // On peut storer
        DoStoreMemberPublicKeyCheckData(publicKeyCheckData);

        // On contrôle si c'est terminé
        CheckIfAllPublicKeyCheckDatasAreReceived();
    }

    private void CheckIfAllPublicKeyCheckDatasAreReceived()
    {
        if (ReceivedMembersPublicKeyCheckDatas.Count(pk => pk != null) == ExpectedPublicKeyCheckDataMembers!.Count)
        {
            WaitForAllPublicKeyCheckDatasReceived.Set();
        }
    }

    /// <summary>
    /// Effectue le store d'une PublicKeyCheckData à la bonne position dans l'ordre des SessionMembers
    /// </summary>
    /// <param name="publicKeyCheckData"></param>
    private void DoStoreMemberPublicKeyCheckData(PublicKeyCheckData publicKeyCheckData)
    {
        int index = ExpectedPublicKeyCheckDataMembers!.IndexOf(publicKeyCheckData.IssuerClientInstanceId);

        if (index >= 0 && index < ExpectedPublicKeyCheckDataMembers.Count)
        {
            ReceivedMembersPublicKeyCheckDatas[index] = publicKeyCheckData;
        }
    }
    
    /// <summary>
    /// Intervient quand la liste des SessionMembers dont on doit récupérer les PublicKeyCheckData est connue
    /// </summary>
    /// <param name="cloudSessionMembersIds"></param>
    public void SetExpectedPublicKeyCheckDataCount(List<string> cloudSessionMembersIds)
    {
        ExpectedPublicKeyCheckDataMembers = cloudSessionMembersIds;
        ReceivedMembersPublicKeyCheckDatas = new PublicKeyCheckData[cloudSessionMembersIds.Count];

        // Si des éléménts étaient en attente, on les traite
        foreach (var publicKeyCheckData in NonStoredPublicKeyCheckDatas)
        {
            DoStoreMemberPublicKeyCheckData(publicKeyCheckData);
        }

        // On contrôle si c'est terminé
        CheckIfAllPublicKeyCheckDatasAreReceived();
    }

    public ReadOnlyCollection<PublicKeyCheckData> GetReceivedPublicKeyCheckData()
    {
        return ReceivedMembersPublicKeyCheckDatas
            .Where(pk => pk != null)
            .Cast<PublicKeyCheckData>()
            .ToList()
            .AsReadOnly();
    }
}