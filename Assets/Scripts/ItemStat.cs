using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemStat 
{
    public float dmgMin, dmgMax;
    public float critMin, critMax;
    public float critDmgMin, critDmgMax;
    public float defMin, defMax;
    public float hpMin, hpMax;
}


//DMG
//ATK SPD(by default it starts at 100% just like how it was in ultra savage, and then if you reach 200% through upgrades and items, the animation is twice as fast etc.)
//CRIT
//CRIT DMG(by default starts at 150%)
//DEF
//HP

//Every level up you get a stat point, and every 5 levels a skill point.Max level will be 300 in this one.For items, we will have 5rarieties: Ordinary/Unique/Rare/Epic/Legendary.And we're returning with the randomized item names
//and we also need the smart stylish way for numbers displayed everywhere:
//1’000        = 1 K
//266’00    = 266K
//5’56’000    = 5,56M
//including dmg numbers on enemies and item stats in tooltips
//Oneshark - LoganToday at 4:44 AM
//We want the kind on the left or right
//Oneshark - OliversToday at 4:45 AM
//the one on the right. So before it reaches one thousand it's displayed in hundreds like normally, but after reaching thousand it starts the new way.
//1341=1.3K
//2394=2.3K
//19405=19.4K