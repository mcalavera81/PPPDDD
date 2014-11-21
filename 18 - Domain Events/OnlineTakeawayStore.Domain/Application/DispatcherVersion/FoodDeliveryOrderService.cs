﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineTakeawayStore.Domain.DispatcherVersion;
using FoodDeliveryOrder = OnlineTakeawayStore.Domain.DispatcherVersion.FoodDeliveryOrder;

namespace OnlineTakeawayStore.Application.DispatcherVersion
{
    public class FoodDeliveryOrderService
    {
        private static int Id = 0;

        // sends real-time notifications to browser and restaurant
        private INotificationChannel clientChannel;
        private IRestaurantConnector connector;
        private IEventDispatcher dispatcher;

        public FoodDeliveryOrderService(INotificationChannel clientChannel, IRestaurantConnector connector,
            IEventDispatcher dispatcher)
        {
            this.clientChannel = clientChannel;
            this.connector = connector;
            this.dispatcher = dispatcher;
        }

        public void PlaceFoodDeliveryOrder(PlaceFoodDeliveryOrderRequest request)
        {
            var id = Id++; // for demonstration purposes only

            dispatcher.Register<FoodDeliveryOrder>(o =>
            {
                clientChannel.Publish("ORDER_ACKNOWLEDGED");
            });

            var order = new FoodDeliveryOrder(
                id, request.CustomerId, request.RestaurantId, request.MenuItemIds,
                request.DeliveryTime
            );

            foreach (var ev in order.RecordedEvents)
            {
                dispatcher.Dispatch(ev);
            }
        }
    }
}