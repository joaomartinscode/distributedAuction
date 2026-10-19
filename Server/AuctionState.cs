using System;
using System.Collections.Generic;
using System.Text;

namespace distributedAuction.Server
{
    internal class AuctionState
    {
        public string ItemName { get; set; }
        public double BasePrice { get; set; }
        public int MaxDuration { get; set; }

        public double HighestBid { get; set; }
        public string HighestBidder { get; set; }

        public volatile bool isStarted;
        public volatile bool isEnded;

        public List<string> ActiveParticipants { get; }


        public AuctionState(string itemName, double basePrice, int maxDuration)
        {
            ItemName = itemName;
            BasePrice = basePrice;
            MaxDuration = maxDuration;

            HighestBid = basePrice;
            HighestBidder = "";
            isStarted = false;
            isEnded = false;

            ActiveParticipants = new List<string>();
        }
    }
}
