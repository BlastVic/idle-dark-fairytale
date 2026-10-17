using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class Dummy : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SetSkinsAndAttachments();
    }

    public void RebirthAnimationStart()
    {
        skeletonAnimation.loop = false;
        skeletonAnimation.AnimationName = "Rebirth";
    }

    public void IdleAnimationStart()
    {
        skeletonAnimation.AnimationName = "Idle1";
        skeletonAnimation.loop = true;
    }


    SkeletonAnimation skeletonAnimation;
    public void SetSkin(string skin)
    {

        skeletonAnimation.skeleton.SetSkin(skin);
    }

    public SpineAttachmentSet GetAllAttachments() //whether empty or not
    {

        SpineAttachmentSet set = new SpineAttachmentSet();
        InventoryManager im = InventoryManager.single;

        if (im.hasWeapon)
        {
            set.attachments.Add(new SpineAttachmentPair { slot = "weapon1", attachment = im.weaponEq.m_AttachmentIfApplicable });
        }
        else
        {
            set.attachments.Add(GetEmptyAttachment(ItemType.WEAPON));
        }

        if (im.hasShield)
        {
            set.attachments.Add(new SpineAttachmentPair { slot = "shield1", attachment = im.shieldEq.m_AttachmentIfApplicable });
        }
        else
        {
            set.attachments.Add(GetEmptyAttachment(ItemType.SHIELD));
        }

        if (im.hasHelmet)
        {
            set.attachments.Add(new SpineAttachmentPair { slot = "Head/Head1", attachment = im.helmetEq.m_AttachmentIfApplicable });
        }
        else
        {
            set.attachments.Add(GetEmptyAttachment(ItemType.HELMET));
        }
        return set;
    }

    public SpineAttachmentPair GetEmptyAttachment(ItemType itemType)
    {
        SpineAttachmentSet sap = new SpineAttachmentSet();

        if (itemType == ItemType.WEAPON)
        {
            return new SpineAttachmentPair { slot = "weapon1", attachment = "Weapons/Wep0" };
            //return new SpineAttachmentPair { slot = "weapon1", attachment = "Empty" };
        }

        if (itemType == ItemType.HELMET)
        {
            return new SpineAttachmentPair { slot = "Head/Head1", attachment = "Empty" };
        }

        if (itemType == ItemType.SHIELD)
        {
            return new SpineAttachmentPair { slot = "shield1", attachment = "Empty" };
            //return new SpineAttachmentPair { slot = "shield1", attachment = "Empty" };
        }

        return null;
    }

    public void SetSkinsAndAttachments()
    {

        SetSkinsAndAttachments(GetAllAttachments());//this is the default behavior if we want to reset the char looks
    }
    public void SetSkinsAndAttachments(SpineAttachmentSet sas = null)
    {
        //Debug.Log("Setting attachments on player");
        if (skeletonAnimation == null || skeletonAnimation.skeleton == null)
        {
            //Debug.Log("You didnt have a skeleton");
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        sas = GetAllAttachments();

        foreach (SpineAttachmentPair sp in sas.attachments)
        {
            skeletonAnimation.skeleton.SetAttachment(sp.slot, sp.attachment);
        }

        if (InventoryManager.single.hasArmor)
        {
            SetSkin(InventoryManager.single.armorEq.m_SkinIfApplicable);
        }
        else
        {
            SetSkin("0");
        }
    }
}
