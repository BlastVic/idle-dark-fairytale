using System;
using System.Collections.Generic;

[System.Serializable]
public class SpineAttachmentSet
{
    public List<SpineAttachmentPair> attachments = new List<SpineAttachmentPair>();
}

[System.Serializable]
public class SpineAttachmentPair
{
    public string slot;
    public string attachment;
}