using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 对崩坏科研3
{
    public class Weapon
    {
        public const int OFFSET_TYPE = -32;
        public const int OFFSET_MAX_STAR = -28;
        public const int OFFSET_STAR = -26;
        public const int OFFSET_ID = 0;
        public const int OFFSET_SKILL1 = 4;
        public const int OFFSET_SKILL2 = 8;
        public const int OFFSET_SKILL3 = 12;
        public const int OFFSET_ACK = 28;
        public const int OFFSET_CRIT = 36;
        public const int OFFSET_DEFENDE = 44;
        public const int OFFSET_HP = 60;
        public const int OFFSET_SKILL1_VALUES = 64;
        public const int OFFSET_SKILL2_VALUES = 88;
        public const int OFFSET_SKILL3_VALUES = 112;
        public int id;
        public int parentID;
        public String? name { get; set; }
        public Int16 star;
        public Int16 maxStar;
        public byte subStar;
        public byte maxSubStar;
        public int[] skillCodes = new int[3];
        public float[][] skillValuesFloat = new float[3][];

        public byte[] skillValuesBytes = new byte[72];
        public float ack;
        public float hp;
        public float defend;
        public float crit;
        public byte type;
        public int address=-1;
        public int addressLow3;
        public byte[]? traitCode;
        public byte[]? allDataBytes;
        public Weapon() { 
            skillValuesFloat[0]= new float[3];
            skillValuesFloat[1] = new float[3];
            skillValuesFloat[2] = new float[3];
            
        }

        public Weapon copy() {
            var w=new Weapon();
            w.id=id;
            w.name=name;
            w.star=star;
            w.maxStar=maxStar;
            w.subStar=subStar;
            w.maxSubStar=maxSubStar;
            w.skillCodes = skillCodes;
            w.skillValuesFloat = skillValuesFloat;
            w.skillValuesBytes = skillValuesBytes;
            w.ack=ack;
            w.hp=hp;
            w.defend=defend;
            w.type=type;
            w.address=address;
            w.traitCode=traitCode;
            w.allDataBytes=allDataBytes;
            return w;

        }
        
    }
}
