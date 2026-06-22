namespace JewelleryBusinessManager.Models;

public enum JewelleryType { Ring, Pendant, Earrings, Bracelet, Necklace, LooseStone, Other }
public enum StockStatus { InStock, Sold, Reserved, AtMarket, ListedOnline, NeedsPhotos, InProgress }
public enum MaterialCategory { OpalRough, CutOpal, Gemstone, Silver, Gold, Chain, Finding, Setting, BezelStrip, Solder, Packaging, DisplaySupply, Consumable, Other }
public enum UnitType { Pieces, Grams, Carats, Metres, Sheets, Millimetres, Other }
public enum JobType { CustomOrder, Repair, Resize, Remake, StoneSetting, CleanAndPolish, MarketPreparation, Other }
public enum JobStatus
{
    Enquiry = 0,
    Quoted = 1,
    Approved = 2,
    DepositPaid = 3,
    AwaitingMaterials = 4,
    InProgress = 5,
    AwaitingCustomerApproval = 6,
    ReadyForPickup = 7,
    ReadyToShip = 8,
    Completed = 9,
    Cancelled = 10,
    Setting = 11,
    Polishing = 12,
    QualityCheck = 13
}
public enum PaymentMethod { Cash, Card, BankTransfer, PayPal, Website, Other }
public enum SaleLocation { Website, Instagram, Market, InPerson, CustomOrder, Other }
public enum StoneStatus
{
    Rough = 0,
    Loose = 1,
    SetInJewellery = 2,
    Sold = 3,
    Reserved = 4,
    Cutting = 5,
    Polished = 6,
    SelectedForDesign = 7,
    AssignedToJewellery = 8
}

public enum ProductionBatchStatus { Planned, MaterialsNeeded, InProgress, ReadyForPhotos, ReadyForMarket, Completed, OnHold, Cancelled }
public enum OnlineListingStatus { NotStarted, NeedsPhotos, NeedsDescription, ReadyToList, Listed, Sold, Archived }
public enum ListingPhotoStatus { NotStarted, NeedsPhotos, PhotosTaken, Edited, Uploaded }

public enum PurchaseOrderStatus { Draft, Ordered, PartiallyReceived, Received, Cancelled }
public enum BusinessTaskStatus { ToDo, InProgress, Waiting, Completed, Cancelled }
public enum BusinessTaskPriority { Low, Normal, High, Urgent }
public enum BusinessTaskCategory { General, BenchWork, CustomerFollowUp, MarketPrep, Inventory, Purchasing, OnlineListing, OpalCutting, Production, Admin }
