public interface IUpgradeable
{
    bool CanUpgrade();
    void Upgrade();
    int GetUpgradeLevel();
    int GetMaxUpgradeLevel();
}
