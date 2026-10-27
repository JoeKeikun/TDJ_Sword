//
//  HapticFeedback.mm
//  iOS触觉反馈原生插件
//  使用UIImpactFeedbackGenerator和UINotificationFeedbackGenerator
//  支持iOS 10+
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

// 检查系统版本是否支持触觉反馈
static BOOL hapticAvailable = NO;
static UIImpactFeedbackGenerator *impactGeneratorLight = nil;
static UIImpactFeedbackGenerator *impactGeneratorMedium = nil;
static UIImpactFeedbackGenerator *impactGeneratorHeavy = nil;
static UISelectionFeedbackGenerator *selectionGenerator = nil;
static UINotificationFeedbackGenerator *notificationGenerator = nil;

static void initHaptic()
{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        // 检查iOS版本，触觉反馈需要iOS 10.0+
        if (@available(iOS 10.0, *)) {
            hapticAvailable = YES;
            
            impactGeneratorLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            impactGeneratorMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            impactGeneratorHeavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            selectionGenerator = [[UISelectionFeedbackGenerator alloc] init];
            notificationGenerator = [[UINotificationFeedbackGenerator alloc] init];
            
            // 预先准备好生成器，减少第一次触发的延迟
            [impactGeneratorLight prepare];
            [impactGeneratorMedium prepare];
            [impactGeneratorHeavy prepare];
            [selectionGenerator prepare];
            [notificationGenerator prepare];
        } else {
            hapticAvailable = NO;
        }
    });
}

extern "C" {

    void _hapticImpactLight()
    {
        initHaptic();
        if (!hapticAvailable) return;
        
        if (@available(iOS 10.0, *)) {
            [impactGeneratorLight impactOccurred];
            [impactGeneratorLight prepare]; // 为下一次准备
        }
    }

    void _hapticImpactMedium()
    {
        initHaptic();
        if (!hapticAvailable) return;
        
        if (@available(iOS 10.0, *)) {
            [impactGeneratorMedium impactOccurred];
            [impactGeneratorMedium prepare];
        }
    }

    void _hapticImpactHeavy()
    {
        initHaptic();
        if (!hapticAvailable) return;
        
        if (@available(iOS 10.0, *)) {
            [impactGeneratorHeavy impactOccurred];
            [impactGeneratorHeavy prepare];
        }
    }

    void _hapticSelectionChanged()
    {
        initHaptic();
        if (!hapticAvailable) return;
        
        if (@available(iOS 10.0, *)) {
            [selectionGenerator selectionChanged];
            [selectionGenerator prepare];
        }
    }

    void _hapticNotificationSuccess()
    {
        initHaptic();
        if (!hapticAvailable) return;
        
        if (@available(iOS 10.0, *)) {
            [notificationGenerator notificationOccurred:UINotificationFeedbackTypeSuccess];
            [notificationGenerator prepare];
        }
    }

    void _hapticNotificationWarning()
    {
        initHaptic();
        if (!hapticAvailable) return;
        
        if (@available(iOS 10.0, *)) {
            [notificationGenerator notificationOccurred:UINotificationFeedbackTypeWarning];
            [notificationGenerator prepare];
        }
    }

    void _hapticNotificationError()
    {
        initHaptic();
        if (!hapticAvailable) return;
        
        if (@available(iOS 10.0, *)) {
            [notificationGenerator notificationOccurred:UINotificationFeedbackTypeError];
            [notificationGenerator prepare];
        }
    }

}
