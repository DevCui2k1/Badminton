using Entities;
using Entities.Models;
using Repositories.Intefaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly DataContext _context;
        private IHistoryTransactionRepository _historyTransaction;
        private IHistoryWalletRepository _historyWallet;
        private IPostRepository _post;
        private IReportRepository _report;
        private IRoleRepository _role;
        private ISlotRepository _slot;
        private ISubscriptionRepository _subscription;
        private ITransactionRepository _transaction;
        private ITypePostRepository _typePost;
        private IUserRatingRepository _userRating;
        private IUserRepository _user;
        private IWalletRepository _wallet;
        private IWishlistRepository _wishlist;
        private IVerifyTokenRepository _verifyToken;
        private ICommentRepository _comment;
        private IUserChatRoomRepository _chatRoomUser;
        private IChatRoomRepository _chatRoom;
        private IMessageRepository _message;
        private INotificationRepository _notification;
        private IHangfireJobRepository _hangfireJob;
        private ISettingRepository _Setting;
        private IWithdrawDetailRepository _WithdrawDetail;
        private ISlotPostRepository _SlotPost;


        public RepositoryManager(DataContext context)
        {
            _context = context;
            _historyTransaction = new HistoryTransactionRepository(_context);
            _historyWallet = new HistoryWalletRepository(_context);
            _post = new PostRepository(_context);
            _report = new ReportRepository(_context);
            _role = new RoleRepository(_context);
            _slot = new SlotRepository(_context);
            _subscription = new SubscriptionRepository(_context);
            _transaction = new TransactionRepository(_context);
            _typePost = new TypePostRepository(_context);
            _userRating = new UserRatingRepository(_context);
            _user = new UserRepository(_context);
            _wallet = new WalletRepository(_context);
            _wishlist = new WishlistRepository(_context);
            _verifyToken = new VerifyTokenRepository(_context);
            _comment = new CommentRepository(_context);
            _chatRoomUser = new UserChatRoomRepository(_context);
            _chatRoom = new ChatRoomRepository(_context);
            _message = new MessageRepository(_context);
            _notification = new NotificationRepository(_context);
            _hangfireJob = new HangfireJobRepository(_context);
            _Setting = new SettingRepository(_context);
            _WithdrawDetail=new WithdrawDetailRepository(_context);
            _SlotPost = new SlotPostRepository(_context);
        }
        public IHistoryTransactionRepository HistoryTransaction
        {
            get
            {
                return _historyTransaction;
            }
        }
        public IHistoryWalletRepository HistoryWallet
        {
            get
            {
                return _historyWallet;
            }
        }
        public IPostRepository Post
        {
            get
            {
                return _post;
            }
        }
        public IReportRepository Report
        {
            get
            {
                return _report;
            }
        }
        public IRoleRepository Role
        {
            get
            {
                return _role;
            }
        }
        public ISlotRepository Slot
        {
            get
            {
                return _slot;
            }
        }
        public ISubscriptionRepository Subscription
        {
            get
            {
                return _subscription;
            }
        }
        public ITransactionRepository Transaction
        {
            get
            {
                return _transaction;
            }
        }
        public ITypePostRepository TypePost
        {
            get
            {
                return _typePost;
            }
        }
        public IUserRatingRepository UserRating
        {
            get
            {
                return _userRating;
            }
        }
        public IUserRepository User
        {
            get
            {
                return _user;
            }
        }
        public IWalletRepository Wallet
        {
            get
            {
                return _wallet;
            }
        }
        public IWishlistRepository Wishlist
        {
            get
            {
                return _wishlist;
            }
        }
        public IVerifyTokenRepository VerifyToken
        {
            get
            {
                return _verifyToken;
            }
        }
        public ICommentRepository Comment
        {
            get
            {
                return _comment;
            }
        }
        public IUserChatRoomRepository ChatRoomUser
        {
            get
            {
                return _chatRoomUser;
            }
        }
        public IChatRoomRepository ChatRoom
        {
            get
            {
                return _chatRoom;
            }
        }
        public IMessageRepository Message
        {
            get
            {
                return _message;
            }
        }

        public INotificationRepository Notification
        {
            get
            {
                return _notification;
            }
        }

        public IHangfireJobRepository HangfireJob
        {
            get
            {
                return _hangfireJob;
            }
        }
        public ISettingRepository Setting
        {
            get
            {
                return _Setting;
            }
        }

        public IWithdrawDetailRepository WithdrawDetail
        {
            get
            {
                return _WithdrawDetail;
            }
        }



        public ISlotPostRepository SlotPost
        {
            get
            {
                return _SlotPost;
            }
        }

        public Task SaveAsync() => _context.SaveChangesAsync();
    }
}
