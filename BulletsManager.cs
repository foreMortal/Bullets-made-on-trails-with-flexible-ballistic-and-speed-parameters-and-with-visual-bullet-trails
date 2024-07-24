using System.Collections.Generic;
using UnityEngine;

public class BulletsManager : MonoBehaviour
{
    //public GameObject tyt;
    [SerializeField] private GetStatisticScriptableObject stats;
    [SerializeField] private GameObject bulletPrefab;

    private Vector3 vec;
    private LayerMask mask = ~0;
    private SetupWeapon weapon;
    private string first, second;
    private List<BulletInfo> activeBullets = new List<BulletInfo>();

    private List<BulletInfo> hiddenBulletsFirst = new List<BulletInfo>();
    private List<BulletInfo> hiddenBulletsSecond = new List<BulletInfo>();

    private List<BulletInfo> bulletsBuffer = new List<BulletInfo>();
    private List<BulletInfo> removeFromBuffer = new List<BulletInfo>();
    private List<BulletInfo> removeList = new List<BulletInfo>();

    private void Awake()
    {
        weapon = GetComponent<SetupWeapon>();

        mask -= 1 << 10;
        mask -= 1 << 11;
        mask -= 1 << 12;
        mask -= 1 << 13;
    }


    public void CreateABullet(BulletInfo bullet, HitInfo hitInfo)
    {
        if (bullet.GunName == first)
        {
            FireNewBullet(hiddenBulletsFirst, activeBullets, bullet, hitInfo);
        }
        else if (bullet.GunName == second)
        {
            FireNewBullet(hiddenBulletsSecond, activeBullets, bullet, hitInfo);
        }
    }

    private void Update()
    {
        if (activeBullets.Count > 0)
        {
            for (int i = 0; i < activeBullets.Count; i++)
            {
                if (activeBullets[i].BulletLifeTime > activeBullets[i].BulletLifeLimit)
                {
                    removeList.Add(activeBullets[i]);
                }
                else
                {
                    activeBullets[i].BulletLifeTime += Time.deltaTime;
                    Vector3 t = activeBullets[i].FiringDirection + activeBullets[i].CameraStartPosition + activeBullets[i].FiringDirection * activeBullets[i].BulletSpeed * activeBullets[i].BulletLifeTime;
                    activeBullets[i].bulletInstance.transform.position = Vector3.MoveTowards(activeBullets[i].bulletInstance.transform.position, t, activeBullets[i].BulletSpeed * Time.deltaTime);

                    activeBullets[i].LastPosition = activeBullets[i].CurrentPosition;
                    activeBullets[i].CurrentPosition = t;

                    vec = activeBullets[i].CurrentPosition - activeBullets[i].LastPosition;

                    if (Physics.Raycast(activeBullets[i].LastPosition, vec.normalized, out RaycastHit hit, activeBullets[i].BulletSpeed * Time.deltaTime, mask))
                    {
                        if (hit.collider.CompareTag("CanGetHitted"))
                        {
                            stats.Hits++;
                            IHitable dummy = hit.collider.GetComponent<IHitable>();
                            if (dummy != null)
                            {
                                dummy.GetHited(activeBullets[i].HitInfo, out bool headShot);
                                if (headShot)
                                {
                                    stats.headShots++;
                                    weapon.RegisterHit();
                                }
                                else
                                {
                                    stats.bodyShots++;
                                    weapon.RegisterHit();
                                }
                            }
                        }
                        else
                        {
                            stats.Misses++;
                        }
                        activeBullets[i].bulletInstance.transform.position = hit.point;
                        removeList.Add(activeBullets[i]);
                        //bulls.Add(Instantiate(tyt, hit.point, Quaternion.identity));
                    }
                }
            }
        }

        if(removeList.Count > 0)
        {
            foreach (var i in removeList)
                GoToBuffer(i);
            removeList.Clear();
        }

        if(bulletsBuffer.Count > 0)
        {
            float time = Time.time;
            foreach (var bul in bulletsBuffer)
            {
                if (time > bul.BulletTimeOut)
                {
                    bul.bulletInstance.SetActive(false);
                    removeFromBuffer.Add(bul);
                }
            }
        }

        if(removeFromBuffer.Count > 0)
        {
            foreach(var bul in removeFromBuffer)
            {
                bulletsBuffer.Remove(bul);

                if (bul.GunName == first)
                    hiddenBulletsFirst.Add(bul);
                else if (bul.GunName == second)
                    hiddenBulletsSecond.Add(bul);
                else
                    Destroy(bul.bulletInstance);
            }
            removeFromBuffer.Clear();
        }
    }

    private void GoToBuffer(BulletInfo info)
    {
        if (info.GunName == first || info.GunName == second)
        {
            activeBullets.Remove(info);
            info.BulletTimeOut = Time.time + 0.1f;
            bulletsBuffer.Add(info);
        }
        else
        {
            activeBullets.Remove(info);
            Destroy(info.bulletInstance);
        }
    }

    private void FireNewBullet(List<BulletInfo> hiddenBullets, List<BulletInfo> activeBullets, BulletInfo bullet, HitInfo hitInfo)
    {
        if (hiddenBullets.Count > 0)
        {
            hiddenBullets[0].bulletInstance.transform.position = bullet.StartPosition;
            hiddenBullets[0].CopyFrom(bullet);
            hiddenBullets[0].HitInfo = hitInfo;
            //hiddenBullets[0].script.SetupBullet(this, bullet, hitInfo);
            activeBullets.Add(hiddenBullets[0]);
            hiddenBullets[0].bulletInstance.SetActive(true);
            hiddenBullets.RemoveAt(0);
        }
        else
        {
            BulletInfo newBullet = new();
            newBullet.CopyFrom(bullet);
            newBullet.bulletInstance = Instantiate(bulletPrefab, bullet.StartPosition, Quaternion.identity);
            newBullet.HitInfo = hitInfo;
            activeBullets.Add(newBullet);
        }
    }

    public void ChangeGun(params string[] gunsNames)
    {
        if (gunsNames[1] == null)
            first = gunsNames[0];

        else
        {
            if (first != gunsNames[0])
            {
                first = gunsNames[0];
                foreach (var b in hiddenBulletsFirst)
                {
                    activeBullets.Remove(b);
                    Destroy(b.bulletInstance);
                }
                hiddenBulletsFirst.Clear();
            }
            if (second != gunsNames[1])
            {
                second = gunsNames[1];
                foreach (var b in hiddenBulletsSecond)
                {
                    activeBullets.Remove(b);
                    Destroy(b.bulletInstance);
                }
                hiddenBulletsSecond.Clear();
            }
        }
    }
}

public class BulletInfo
{
    public string id;
    public HitInfo HitInfo;
    public Transform firePoint;
    public int HideIterations;
    public string GunName;
    public GameObject bulletInstance;
    public Vector3 StartPosition;
    public Vector3 CameraStartPosition;
    public Vector3 LastPosition, CurrentPosition;
    public Quaternion CameraStartRotation;
    public Vector3 FiringDirection;
    public float BulletTimeOut;
    public float BulletLifeTime;
    public float BulletLifeLimit;
    public float BulletSpeed;

    public BulletInfo() { }

    public override bool Equals(object obj)
    {
        if(obj == null || obj is not BulletInfo)
            return false;
        else
            return id == ((BulletInfo)obj).id;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }

    public void CopyFrom(BulletInfo info)
    {
        firePoint = info.firePoint;
        id = info.id;
        HideIterations = 0;
        GunName = info.GunName;
        CurrentPosition = info.CameraStartPosition;
        StartPosition = info.StartPosition;
        CameraStartPosition = info.CameraStartPosition;
        FiringDirection = info.FiringDirection;
        BulletLifeTime = 0f;
        BulletLifeLimit = info.BulletLifeLimit;
        BulletSpeed = info.BulletSpeed;
    }

    public BulletInfo(string GunName, Vector3 StartPosition, Vector3 CameraStartPosition, Quaternion CameraStartRotation, Vector3 FiringDirection, float BulletLifeLimit, float BulletSpeed)
    {
        CurrentPosition = CameraStartPosition;
        HideIterations = 0;
        this.GunName = GunName;
        this.StartPosition = StartPosition;
        this.CameraStartPosition = CameraStartPosition;
        this.CameraStartRotation = CameraStartRotation;
        this.FiringDirection = FiringDirection;
        this.BulletLifeLimit = BulletLifeLimit;
        this.BulletSpeed = BulletSpeed;
    }

    public void CreateNewBullet(string GunName, Vector3 StartPosition, Vector3 CameraStartPosition, Quaternion CameraStartRotation, Vector3 FiringDirection, float BulletLifeLimit, float BulletSpeed, float id)
    {
        this.id = id.ToString();
        CurrentPosition = CameraStartPosition;
        HideIterations = 0;
        this.GunName = GunName;
        this.StartPosition = StartPosition;
        this.CameraStartPosition = CameraStartPosition;
        this.CameraStartRotation = CameraStartRotation;
        this.FiringDirection = FiringDirection;
        this.BulletLifeLimit = BulletLifeLimit;
        this.BulletSpeed = BulletSpeed;
    }
}
