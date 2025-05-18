
package com.yourcompany.plugin;

import android.app.Activity;
import android.content.Intent;

public class Share {
    public static void shareText(Activity activity, String message) {
        Intent sendIntent = new Intent();
        sendIntent.setAction(Intent.ACTION_SEND);
        sendIntent.putExtra(Intent.EXTRA_TEXT, message);
        sendIntent.setType("text/plain");
        activity.startActivity(Intent.createChooser(sendIntent, "Share via"));
    }
}
