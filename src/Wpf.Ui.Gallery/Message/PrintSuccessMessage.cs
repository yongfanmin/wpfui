// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using CommunityToolkit.Mvvm.Messaging.Messages;
using Wpf.Ui.Gallery.Dto.Picking;

namespace Wpf.Ui.Gallery.Message;

public class PrintSuccessMessage : ValueChangedMessage<OrderPick>
{
    public PrintSuccessMessage(OrderPick value) : base(value)
    {
    }
}