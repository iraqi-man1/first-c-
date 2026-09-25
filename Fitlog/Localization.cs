using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;

namespace Fitlog;
public sealed partial class MainWindow
{
    private bool Arabic => Pref.Language == "ar";
    private CultureInfo UiCulture => CultureInfo.GetCultureInfo(Arabic ? "ar-IQ" : "en-US");
    private string Tr(string text)
    {
        if (!Arabic || string.IsNullOrEmpty(text)) return text;
        if (ArabicStrings.TryGetValue(text, out var result)) return result;
        foreach (var prefix in new[] { "＋  ", "↓  ", "↑  ", "✓  ", "●   " })
            if (text.StartsWith(prefix)) return prefix + Tr(text[prefix.Length..]);
        if (text.Contains(" · ")) return string.Join(" · ", text.Split(" · ").Select(Tr));
        if (text.StartsWith("Invalid ") && text.EndsWith("values. Check the entered ranges.")) return "قيم غير صالحة. تحقق من الأرقام والحقول المدخلة.";
        return text;
    }
    private string L(string english, string arabic) => Arabic ? arabic : english;
    private string DateText(DateOnly day, string format = "dd MMMM yyyy") => day.ToString(format, UiCulture);
    private string AttendanceText(Models.DailyLog entry) => Tr(entry.EffectiveStatus switch
    {
        Models.Attendance.Training => "Training day", Models.Attendance.Rest => "Rest day", Models.Attendance.Missed => "Missed gym", _ => "Not specified"
    });
    private string LogSummary(Models.DailyLog entry) => $"{AttendanceText(entry)}{(entry.EffectiveWorkoutCompletion is { } p ? $" · {Tr("Workout completion")} {p}%" : "")} · {Tr("Diet adherence")} {entry.EffectiveDietCompletion}%";
    private ComboBox SelectOptions(string[] options, int index)
    {
        return new ComboBox { ItemsSource = options.Select(x => new ComboBoxItem { Content = Tr(x), Tag = x }).ToArray(), SelectedIndex = index, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
    }
    private static readonly Dictionary<string, string> ArabicStrings = """
        Dashboard|لوحة المتابعة
        Calendar|التقويم
        Workouts|جدول التمارين
        Weight|الوزن
        Measurements|القياسات
        Progress Photos|صور التقدم
        Goals & Records|الأهداف والإنجازات
        Journey|رحلتي
        Statistics|الإحصائيات
        Settings|الإعدادات
        Saved on this device|محفوظ على هذا الجهاز
        Open a date|اختر تاريخاً
        Switch appearance|تغيير المظهر
        Log Today|سجّل اليوم
        Saved to your device|تم الحفظ على جهازك
        Save entry|حفظ السجل
        Save|حفظ
        Cancel|إلغاء
        Close|إغلاق
        Delete|حذف
        Edit|تعديل
        Update|تحديث
        Remove|إزالة
        Back|رجوع
        Next|التالي
        Previous|السابق
        Unable to complete|تعذّر إكمال العملية
        Delete this entry?|حذف هذا السجل؟
        This removes the selected entry from this device.|سيتم حذف السجل المحدد من هذا الجهاز.
        Entry deleted|تم حذف السجل
        Today|اليوم
        A little progress, every day. Start with a check-in.|خطوة صغيرة كل يوم. ابدأ بتسجيل يومك.
        Training day|يوم تمرين
        Rest day|يوم راحة
        Missed gym|ما رحت للجم
        Not specified|غير محدد
        Workout completion|نسبة إنجاز التمرين
        THIS WEEK|هذا الأسبوع
        Weekly gym goal|هدف التمارين الأسبوعي
        Diet followed|التزمت بالأكل
        Yearly activity|النشاط السنوي
        Every square is a day. Select one to see the full story.|كل مربع يمثل يوماً. اضغط عليه لعرض سجله.
        Overall|الكل
        Gym|النادي
        Diet|الأكل
        Weight logging|تسجيل الوزن
        Less|أقل
        More|أكثر
        Current weight|الوزن الحالي
        Gym this month|تمارين هذا الشهر
        Current gym streak|أيام التمرين المتتالية
        Diet adherence|نسبة الالتزام بالأكل
        of logged days this month|من الأيام المسجلة هذا الشهر
        Weight trend|تغيّر الوزن
        Your goals|أهدافك
        Target weight|الوزن المستهدف
        Monthly gym goal|هدف التمارين الشهري
        View goals  →|عرض الأهداف ←
        DAY BY DAY|يوماً بيوم
        A clear view of every check-in, rest day, and weigh-in.|سجلاتك اليومية وأيام الراحة والوزن في مكان واحد.
        BODY / TREND|الجسم / التغيّر
        Weight progress|تقدم الوزن
        Track change at your own pace. Weigh-ins are always optional.|تابع تغيراتك على راحتك. تسجيل الوزن اختياري دائماً.
        Add weight|إضافة وزن
        Edit weight|تعديل الوزن
        Starting weight|وزن البداية
        Lowest|أقل وزن
        Highest|أعلى وزن
        HISTORY|السجل
        Week|أسبوع
        Month|شهر
        3 months|٣ أشهر
        6 months|٦ أشهر
        Year|سنة
        All|الكل
        Total change|التغير الكلي
        Last 7 days|آخر ٧ أيام
        Last 30 days|آخر ٣٠ يوماً
        Last 90 days|آخر ٩٠ يوماً
        Weight history|سجل الوزن
        No weigh-ins yet. Add your first weight to start your trend.|لا توجد أوزان مسجلة. أضف أول وزن لبدء المتابعة.
        BEYOND THE SCALE|أكثر من مجرد وزن
        Notice the changes that a number on the scale can miss.|لاحظ تغيرات جسمك التي لا تظهر على الميزان.
        Add measurements|إضافة قياسات
        Waist|الخصر
        Chest|الصدر
        Hips|الورك
        Measurement history|سجل القياسات
        No measurements yet. All measurements are optional.|لا توجد قياسات بعد. تسجيل القياسات اختياري.
        YOUR STORY|قصتك
        The effort, the rest days, and everything in between.|التمرين والراحة وتفاصيل رحلتك اليومية.
        Add check-in|إضافة سجل يومي
        Every journey has a day one|لكل رحلة يوم أول
        Use your daily check-in to record how you feel, your training, and your notes.|سجّل تمرينك وشعورك وملاحظاتك اليومية.
        THE BIGGER PICTURE|الصورة الكاملة
        Your consistency, measured over time. Totals use your recorded check-ins.|متابعة التزامك بمرور الوقت. الحسابات تعتمد على الأيام المسجلة.
        Logged days|الأيام المسجلة
        Gym sessions|جلسات التمرين
        Total steps|مجموع الخطوات
        Average sleep|متوسط النوم
        Average water|متوسط شرب الماء
        per logged day|لكل يوم مسجل
        Monthly activity|النشاط الشهري
        Daily check-in|السجل اليومي
        Small actions add up. Record what matters to you.|الخطوات الصغيرة تصنع الفرق. سجّل ما يهمك.
        Day status|حالة اليوم
        Choose a day status|اختر حالة اليوم
        Completion · %|نسبة الإنجاز · %
        Diet adherence · %|نسبة الالتزام بالأكل · %
        Workout|التمرين
        e.g. Upper body · strength|مثلاً: تمارين الجزء العلوي
        Duration · minutes|المدة · دقائق
        Steps|الخطوات
        Water · liters|الماء · لتر
        Sleep · hours|النوم · ساعات
        Energy · 1 to 5|الطاقة · من ١ إلى ٥
        Notes|ملاحظات
        How did today feel?|كيف كان يومك؟
        Add a weigh-in for this day|إضافة وزن لهذا اليوم
        Delete check-in|حذف سجل اليوم
        Delete check-in?|حذف سجل اليوم؟
        The check-in will be removed. Weigh-ins and photos are kept separately.|سيتم حذف سجل اليوم. تبقى الأوزان والصور محفوظة بشكل منفصل.
        Check-in deleted|تم حذف سجل اليوم
        A day at a time|يوم بيوم
        Check-ins can be recorded for today or any previous day.|يمكن التسجيل لليوم أو لأي يوم سابق.
        WEIGH-IN|تسجيل الوزن
        One weigh-in per day. Saving the same date updates its value.|وزن واحد لكل يوم. الحفظ بنفس التاريخ يحدّث القيمة.
        Date|التاريخ
        BODY MEASUREMENTS|قياسات الجسم
        Record measurements|تسجيل القياسات
        Use a consistent measuring point each time.|استخدم نفس موضع القياس في كل مرة.
        Make fitlog your own. Your data is stored locally on this device.|اضبط التطبيق على راحتك. بياناتك محفوظة محلياً على هذا الجهاز.
        Appearance|المظهر
        Dark|داكن
        Light|فاتح
        Language|اللغة
        English|English
        العربية|العربية
        Weight units|وحدة الوزن
        Measurement units|وحدة القياس
        kg|كغم
        lb|رطل
        cm|سم
        in|إنش
        Week starts on|بداية الأسبوع
        Monday|الاثنين
        Tuesday|الثلاثاء
        Wednesday|الأربعاء
        Thursday|الخميس
        Friday|الجمعة
        Saturday|السبت
        Sunday|الأحد
        Tracking preferences|تفضيلات المتابعة
        Heatmap default|العرض الافتراضي للنشاط
        Show optional check-in fields|الحقول الاختيارية في السجل اليومي
        Energy|الطاقة
        Sleep hours|ساعات النوم
        Water intake|شرب الماء
        Hidden fields keep their existing values.|إخفاء الحقول لا يحذف قيمها المحفوظة.
        Goals|الأهداف
        Diet goal · %|هدف الالتزام بالأكل · %
        Save goals|حفظ الأهداف
        Goals saved|تم حفظ الأهداف
        Preferences saved|تم حفظ التفضيلات
        Backup & export|النسخ الاحتياطي والتصدير
        Keep a portable copy of your check-ins, weight, goals, photos, and preferences.|احتفظ بنسخة من سجلاتك ووزنك وأهدافك وصورك وجدولك وإعداداتك.
        Export backup|تصدير نسخة احتياطية
        Import backup|استيراد نسخة احتياطية
        Import merges entries. Matching dates and IDs are updated; other records are kept.|الاستيراد يدمج السجلات ويحدّث المتطابق منها ويحافظ على البقية.
        Local storage|التخزين المحلي
        No account needed. Photos are included in your local database and backups.|لا تحتاج حساباً. الصور محفوظة داخل قاعدة البيانات والنسخ الاحتياطية.
        Export fitlog backup|تصدير نسخة احتياطية من fitlog
        Import fitlog backup|استيراد نسخة احتياطية إلى fitlog
        Fitlog backup|نسخة fitlog احتياطية
        Backup exported|تم تصدير النسخة الاحتياطية
        Backup imported. A recovery copy was saved in the Backups folder.|تم الاستيراد وحفظ نسخة استرجاع في مجلد Backups.
        KEEP MOVING FORWARD|واصل التقدم
        Set your own milestones and celebrate your progress.|حدد أهدافك واحتفل بتقدمك.
        Add goal|إضافة هدف
        Current streak|الأيام المتتالية
        Make it personal|هدف يناسبك
        Set a strength, distance, or consistency goal and update your progress as you go.|حدد هدفاً للقوة أو المسافة أو الالتزام وحدّث تقدمك باستمرار.
        Add your first goal|أضف هدفك الأول
        Goal achieved|تم تحقيق الهدف
        Your goal|هدفك
        PERSONAL MILESTONE|إنجاز شخصي
        Update your goal|تحديث الهدف
        Choose a measurable goal that matters to you.|اختر هدفاً قابلاً للقياس يهمك.
        Goal name|اسم الهدف
        Target|المستهدف
        Current progress|التقدم الحالي
        Unit · kg, km, sessions…|الوحدة · كغم، كم، جلسات…
        e.g. Deadlift personal record|مثلاً: رقم شخصي في الديدلفت
        YOUR PLAN|خطتك
        Workout schedule|جدول التمارين
        Your weekly plan and custom exercises. Record attendance from Log Today.|جدولك الأسبوعي وتمارينك المخصصة. سجّل حضورك من زر سجّل اليوم.
        Add workout day|إضافة يوم تمرين
        Create your training plan|أنشئ جدول تمارينك
        Add a workout, choose its weekdays, and list the exercises you follow.|أضف جلسة وحدد أيامها والتمارين التي تلتزم بها.
        Workout name|اسم الجلسة
        e.g. Push day|مثلاً: يوم الدفع
        Repeat on|يتكرر في
        Exercises|التمارين
        Add exercise|إضافة تمرين
        Exercise name|اسم التمرين
        Sets|الجولات
        Reps|التكرارات
        Load|الوزن المستخدم
        Rest · seconds|الراحة · ثواني
        Schedule notes|ملاحظات الجدول
        Edit workout|تعديل الجلسة
        Scheduled today|مقرر اليوم
        Add at least one exercise.|أضف تمريناً واحداً على الأقل.
        Choose at least one weekday.|اختر يوماً واحداً على الأقل.
        Enter a workout name.|أدخل اسم الجلسة.
        Enter an exercise name and rep range.|أدخل اسم التمرين وعدد التكرارات.
        A DIFFERENT PERSPECTIVE|زاوية مختلفة
        Your progress, in your own time. Photos stay on this device.|شاهد تقدمك على راحتك. الصور تبقى على هذا الجهاز.
        Albums|الألبومات
        New album|ألبوم جديد
        Album name|اسم الألبوم
        Album date|تاريخ الألبوم
        Create album|إنشاء ألبوم
        All photos|كل الصور
        Ungrouped photos|صور بدون ألبوم
        Add photos|إضافة صور
        Add to album|إضافة إلى ألبوم
        Move to album|نقل إلى ألبوم
        Compare photos|مقارنة الصور
        Select for comparison|اختيار للمقارنة
        Clear selection|إلغاء الاختيار
        Select exactly two photos to compare.|اختر صورتين للمقارنة.
        Two photos selected. Clear one before selecting another.|اخترت صورتين. ألغِ اختيار إحداهما لإضافة صورة أخرى.
        See how far you have come|شاهد المسافة التي قطعتها
        Add photos or create an album to collect a whole progress session.|أضف صوراً أو أنشئ ألبوماً يجمع صور جلسة تقدم كاملة.
        This album is empty|هذا الألبوم فارغ
        Add multiple photos to keep them together in this album.|أضف عدة صور لتجميعها داخل هذا الألبوم.
        Choose photos|اختيار الصور
        Choose progress photos|اختر صور التقدم
        Photos|الصور
        Caption|تعليق
        A note about this moment|ملاحظة عن هذه اللحظة
        Album|الألبوم
        Without album|بدون ألبوم
        Delete photo|حذف الصورة
        Open photo|فتح الصورة
        Photo viewer|عارض الصور
        Zoom|تكبير
        Fit|ملاءمة
        Before|قبل
        After|بعد
        Select up to 20 photos at a time.|يمكن إضافة ٢٠ صورة في المرة الواحدة.
        Please choose images smaller than 10 MB each.|اختر صوراً أصغر من ١٠ ميغابايت لكل صورة.
        This photo could not be displayed.|تعذّر عرض هذه الصورة.
        Enter an album name.|أدخل اسم الألبوم.
        Enter all numeric values before saving.|أدخل كل القيم الرقمية قبل الحفظ.
        Choose a date on or before today.|اختر تاريخ اليوم أو يوماً سابقاً.
        This backup is too large (maximum 150 MB).|حجم النسخة كبير جداً (الحد الأقصى ١٥٠ ميغابايت).
        This is not a complete fitlog backup.|هذا الملف ليس نسخة احتياطية كاملة من fitlog.
        Unsupported or incomplete backup.|نسخة احتياطية غير مدعومة أو غير مكتملة.
        A photo refers to a missing album.|هناك صورة مرتبطة بألبوم غير موجود.
        """.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim().Split('|', 2)).ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
}
