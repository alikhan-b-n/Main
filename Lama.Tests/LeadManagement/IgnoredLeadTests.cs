using Lama.Domain.LeadManagement.Entities;

namespace Lama.Tests.LeadManagement;

public class IgnoredLeadTests
{
    private static Lead NewLead() => Lead.Create(LeadSamples.Submission());

    [Fact]
    public void AnIgnoredLeadIsNoLongerOpen()
    {
        var lead = NewLead();

        lead.ChangeStatus(LeadStatus.Ignored);

        Assert.Equal(LeadStatus.Ignored, lead.Status);
        Assert.False(lead.IsOpen);
    }

    [Fact]
    public void IgnoringNeedsNoReason()
    {
        var lead = NewLead();

        // Losing a deal has to be explained; ignoring spam does not
        lead.ChangeStatus(LeadStatus.Ignored);

        Assert.Null(lead.LostReason);
    }

    [Fact]
    public void AnIgnoredLeadCanBeTakenBack()
    {
        var lead = NewLead();
        lead.ChangeStatus(LeadStatus.Ignored);

        lead.ChangeStatus(LeadStatus.New);

        Assert.True(lead.IsOpen);
    }

    [Fact]
    public void TheChangeIsRecordedInTheTimeline()
    {
        var lead = NewLead();

        lead.ChangeStatus(LeadStatus.Ignored);

        var entry = lead.Events.Last();
        Assert.Equal(LeadEventKind.StatusChanged, entry.Kind);
        Assert.Equal(LeadStatus.New, entry.FromStatus);
        Assert.Equal(LeadStatus.Ignored, entry.ToStatus);
    }
}
