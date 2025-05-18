
#import <UIKit/UIKit.h>

extern "C" {
    void ShareText(const char* message) {
        NSString* text = [NSString stringWithUTF8String:message];
        NSArray* items = @[text];
        UIActivityViewController* activityVC = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
        UIViewController* rootVC = UIApplication.sharedApplication.keyWindow.rootViewController;
        [rootVC presentViewController:activityVC animated:YES completion:nil];
    }
}
