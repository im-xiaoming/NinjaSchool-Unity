using System.Collections.Generic;

namespace NsoServer.World
{
    // Port of Char.initListCanEnterMap: maps unlock as the main quest (taskId) advances.
    // Maps with id >= 72, the Haruna dormitory and -1 are always allowed (Char.isCanEnterMap).
    public static class MapUnlock
    {
        public static List<int> Allowed(int taskId)
        {
            var list = new List<int>();
            list.Add(22); // MapName.LANG_TONE
            if (taskId >= 3)
            { // TaskName.NV_DIET_SEN_TRU_COC
                list.Add(21); // MapName.DOI_FUMIMEN
                list.Add(23); // MapName.VACH_ICHIDAI
            }

            if (taskId >= 5)
            { // TaskName.NV_HAI_THUOC_CUU_NGUOI
                list.Add(6); // MapName.THAC_KITAJIMA
                list.Add(20); // MapName.CHAN_THAC_KITAJIMA
            }

            if (taskId >= 6)
            { // TaskName.NV_KHAM_PHA_XA_LANG
                list.Add(25); // MapName.DOI_KOKORO
                list.Add(69); // MapName.VACH_AINODAKE
                list.Add(70); // MapName.THUNG_LUNG_CHET
                list.Add(2); // MapName.KHU_LUYEN_TAP
                list.Add(71); // MapName.RUNG_GIA
                list.Add(26); // MapName.CANH_DONG_FUKI
            }

            if (taskId >= 8)
            { // TaskName.NV_TIM_HIEU_3_TRUONG
                list.Add(27); // MapName.TRUONG_HARUNA
                list.Add(1); // MapName.TRUONG_HIROSAKI
                list.Add(72); // MapName.TRUONG_OOKAZA
            }

            if (taskId >= 10)
            { // TaskName.NV_BAI_HOC_DAU_TIEN
                list.Add(3); // MapName.DONG_HACHI
                list.Add(4); // MapName.RUNG_DAO_SAKURA
                list.Add(28); // MapName.KY_TUC_XA_HARUNA
                list.Add(60); // MapName.CUA_HANG_AKA
                list.Add(39); // MapName.SONG_BANG_YAMATO
                list.Add(46); // MapName.HO_STUKI
            }

            if (taskId >= 14)
            { // TaskName.NV_THU_THAP_NGUYEN_LIEU
                list.Add(29); // MapName.HANG_AKA
                list.Add(40); // MapName.CANH_DONG_HIYA
                list.Add(5); // MapName.RUNG_TRUC_UTRA
            }

            if (taskId >= 15)
            { // TaskName.NV_TRUYEN_TAI_TIN_TUC
                list.Add(7); // MapName.RUNG_MISHIMA
                list.Add(8); // MapName.SONG_WATAMARO
                list.Add(65); // MapName.MUI_NURANURA
                list.Add(30); // MapName.SUOI_AKAGI
                list.Add(31); // MapName.BO_BIEN_OURA
            }

            if (taskId >= 16)
            { // TaskName.NV_REN_LUYEN_THE_LUC
                list.Add(63); // MapName.HANG_HA
                list.Add(47); // MapName.HEM_NUI_TAKANA
                list.Add(9); // MapName.NGHIA_DIA_IZUKO
            }

            if (taskId >= 17)
            { // TaskName.NV_DUA_JAIAN_TRO_VE
                list.Add(32); // MapName.LANG_CHAI
                list.Add(61); // MapName.CUA_BIEN_KAWAGUCHI
                list.Add(33); // MapName.RUNG_MOSHIO
            }

            if (taskId >= 18)
            { // TaskName.NV_TIM_NGUYEN_LIEU_LAM_THUOC
                list.Add(48); // MapName.LANG_OSHIN
                list.Add(50); // MapName.RUNG_KANASHII
                list.Add(10); // MapName.LANG_KOJIN
                list.Add(11); // MapName.MIEU_KAMO
            }

            if (taskId >= 19)
            { // TaskName.NV_LAY_NUOC_HANG_SAU
                list.Add(49); // MapName.DEN_AMATERASU
            }

            if (taskId >= 21)
            { // TaskName.NV_VUOT_QUA_THU_THACH
                list.Add(12); // MapName.MIEU_OBOKO
                list.Add(34); // MapName.DAO_HEBI
                list.Add(51); // MapName.RUNG_TOGE
                list.Add(57); // MapName.SAN_SAU_MIEU_OBOKO
            }

            if (taskId >= 22)
            { // TaskName.NV_THU_THAP_CHIA_KHOA
                list.Add(35); // MapName.HANG_MEIRO
            }

            if (taskId >= 24)
            { // TaskName.NV_TRUY_TIM_BAO_VAT
                list.Add(52); // MapName.RUNG_KAPPA
                list.Add(66); // MapName.KHE_NUI_CHOROCHORO
            }

            if (taskId >= 25)
            { // TaskName.NV_REN_LUYEN
                list.Add(13); // MapName.RUNG_GO_KOUJI
            }

            if (taskId >= 26)
            { // TaskName.NV_THU_THAP_TINH_THE_BANG
                list.Add(64); // MapName.HANG_KUGYOU
            }

            if (taskId >= 27)
            { // TaskName.NV_THU_THAP_XAC_DOI_LUA
                list.Add(14); // MapName.RUNG_AOKIGAHARA
            }

            if (taskId >= 28)
            { // TaskName.NV_KIEN_TRI_DIET_AC
                list.Add(38); // MapName.LANG_CHAKUMI
                list.Add(15); // MapName.VACH_NUI_ITO
                list.Add(68); // MapName.NUI_ANZEN
                list.Add(67); // MapName.NUI_ONTAKE
                list.Add(16); // MapName.THUNG_LUNG_TAIRA
            }

            if (taskId >= 31)
            { // TaskName.NV_DU_TRU_LUONG_THUC
                list.Add(68); // MapName.NUI_ANZEN
                list.Add(67); // MapName.NUI_ONTAKE
                list.Add(16); // MapName.THUNG_LUNG_TAIRA
            }

            if (taskId >= 30)
            { // TaskName.NV_TUAN_HOAN
                list.Add(17); // MapName.LANG_SANZU
                list.Add(41); // MapName.KHU_DA_DO_AKAI
                list.Add(43); // MapName.LANG_ECHIGO
                list.Add(42); // MapName.KHU_DA_DO_AIKO
                list.Add(59); // MapName.MUI_HONE
                list.Add(24); // MapName.DINH_ICHIDAI
                list.Add(36); // MapName.DONG_KISEI
                list.Add(37); // MapName.NUI_HASHIGOTO
                list.Add(62); // MapName.HANG_CHI
                list.Add(44); // MapName.DINH_OKAMA
                list.Add(45); // MapName.HANG_NUI_KURAI
                list.Add(53); // MapName.DONG_TAMATAMO
                list.Add(54); // MapName.DEN_HARUMOTO
                list.Add(55); // MapName.PHONG_AN_OUNIO
                list.Add(18); // MapName.SAN_DEN_OROCHI
                list.Add(19); // MapName.NGOI_DEN_OROCHI
                list.Add(58); // MapName.SAN_SAU_DEN_OROCHI
            }

            return list;
        }

        public static bool CanEnter(int taskId, int map)
        {
            if (map == 0 || map >= 72 || map == -1)
                return true;
            return Allowed(taskId).Contains(map);
        }
    }
}
